using System.Globalization;
using Acorn.Dataverse.Plugins.Registration;

namespace Acorn.Dataverse.Cli.Planning;

/// <summary>
/// Compares what the package declares with what exists in the environment and decides what to
/// create, update or delete. Pure: no I/O, so every decision is unit-testable.
/// </summary>
internal static class DeploymentPlanner
{
    public static DeploymentPlan Create(DesiredState desired, EnvironmentState current, bool prune)
    {
        var errors = new List<string>();
        var package = PlanPackage(desired, current.Package, errors);

        var remaining = current.Steps.ToList();
        var changes = new List<StepChange>();

        foreach (var plugin in desired.PluginTypes)
        {
            foreach (var step in plugin.Steps)
            {
                var images = plugin.Images
                    .Where(image => image.StepId is null || string.Equals(image.StepId, step.Id, StringComparison.Ordinal))
                    .ToList();

                var key = StepKey(plugin.TypeName, step);
                var existing = remaining.FirstOrDefault(s => string.Equals(StepKey(s), key, StringComparison.OrdinalIgnoreCase));

                if (existing is null)
                {
                    changes.Add(PlanNewStep(plugin.TypeName, step, images));
                    continue;
                }

                remaining.Remove(existing);
                changes.Add(PlanExistingStep(plugin.TypeName, step, images, existing, prune));
            }
        }

        foreach (var orphan in remaining)
        {
            changes.Add(new StepChange(
                prune ? ChangeKind.Delete : ChangeKind.Orphan, orphan.TypeName, orphan.Name, null, orphan, [], []));
        }

        return new DeploymentPlan(package, changes, errors);
    }

    /// <summary>Step name: the declared one, or the Plugin Registration Tool format.</summary>
    internal static string StepName(string typeName, PluginStepDefinition step)
        => step.Name ?? $"{typeName}: {step.MessageName} of {(IsNone(step.PrimaryEntityName) ? "any Entity" : step.PrimaryEntityName)}";

    /// <summary>Step description: the declared one, or the step name, as the Plugin Registration Tool does.</summary>
    internal static string StepDescription(string typeName, PluginStepDefinition step)
        => step.Description ?? StepName(typeName, step);

    private static PackageChange PlanPackage(DesiredState desired, PackageState? current, List<string> errors)
    {
        if (current is null)
        {
            return new PackageChange(ChangeKind.Create, desired.PackageUniqueName, desired.PackageVersion);
        }

        if (NormalizeVersion(current.Version) != NormalizeVersion(desired.PackageVersion))
        {
            errors.Add(
                $"Package '{current.UniqueName}' is version {current.Version} in the environment, but the .nupkg is " +
                $"{desired.PackageVersion}. Dataverse does not allow changing a package version: keep the package " +
                "version fixed and version the assemblies instead.");
        }

        return new PackageChange(ChangeKind.Update, current.UniqueName, current.Version);
    }

    private static StepChange PlanNewStep(string typeName, PluginStepDefinition step, IReadOnlyList<PluginImageDefinition> images)
        => new(
            ChangeKind.Create,
            typeName,
            StepName(typeName, step),
            step,
            null,
            [],
            images.Select(image => new ImageChange(ChangeKind.Create, image.Alias, image, null, [])).ToList());

    private static StepChange PlanExistingStep(
        string typeName,
        PluginStepDefinition step,
        IReadOnlyList<PluginImageDefinition> images,
        StepState current,
        bool prune)
    {
        var differences = new List<string>();
        Compare(differences, "name", current.Name, StepName(typeName, step));
        Compare(differences, "execution order",
            current.ExecutionOrder.ToString(CultureInfo.InvariantCulture),
            step.ExecutionOrder.ToString(CultureInfo.InvariantCulture));
        Compare(differences, "filtering attributes",
            JoinAttributes(current.FilteringAttributes), JoinAttributes(step.FilteringAttributes), ignoreCase: true);
        Compare(differences, "description", current.Description, StepDescription(typeName, step));
        Compare(differences, "unsecure configuration", current.UnsecureConfiguration, step.UnsecureConfiguration);
        Compare(differences, "run as", current.RunAsUser, step.RunAsUser, ignoreCase: true);
        Compare(differences, "async auto delete", Flag(current.AsyncAutoDelete), Flag(step.AsyncAutoDelete));

        return new StepChange(
            differences.Count == 0 ? ChangeKind.Unchanged : ChangeKind.Update,
            typeName,
            StepName(typeName, step),
            step,
            current,
            differences,
            PlanImages(step, images, current, prune));
    }

    private static List<ImageChange> PlanImages(
        PluginStepDefinition step, IReadOnlyList<PluginImageDefinition> desired, StepState current, bool prune)
    {
        var remaining = current.Images.ToList();
        var changes = new List<ImageChange>();

        foreach (var image in desired)
        {
            var existing = remaining.FirstOrDefault(i => string.Equals(i.Alias, image.Alias, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                changes.Add(new ImageChange(ChangeKind.Create, image.Alias, image, null, []));
                continue;
            }

            remaining.Remove(existing);

            var differences = new List<string>();
            Compare(differences, "type", existing.ImageType.ToString(), image.ImageType.ToString());
            Compare(differences, "name", existing.Name, image.Name ?? image.Alias);
            Compare(differences, "attributes",
                JoinAttributes(existing.Attributes), JoinAttributes(image.Attributes), ignoreCase: true);
            Compare(differences, "message property",
                existing.MessagePropertyName, image.MessagePropertyName ?? MessagePropertyNames.For(step.MessageName));

            changes.Add(new ImageChange(
                differences.Count == 0 ? ChangeKind.Unchanged : ChangeKind.Update, image.Alias, image, existing, differences));
        }

        foreach (var orphan in remaining)
        {
            changes.Add(new ImageChange(prune ? ChangeKind.Delete : ChangeKind.Orphan, orphan.Alias, null, orphan, []));
        }

        return changes;
    }

    // Natural key: what makes two steps "the same step" for Dataverse.
    private static string StepKey(string typeName, PluginStepDefinition step)
        => Key(typeName, step.MessageName, step.PrimaryEntityName, step.SecondaryEntityName, step.Stage, step.Mode);

    private static string StepKey(StepState step)
        => Key(step.TypeName, step.MessageName, step.PrimaryEntityName, step.SecondaryEntityName, step.Stage, step.Mode);

    private static string Key(
        string typeName, string messageName, string? primary, string? secondary, PipelineStage stage, ExecutionMode mode)
        => $"{typeName}|{messageName}|{EntityKey(primary)}|{EntityKey(secondary)}|{(int)stage}|{(int)mode}";

    // Dataverse stores "any entity" as "none".
    private static string EntityKey(string? entity) => IsNone(entity) ? "none" : entity!;

    private static bool IsNone(string? entity)
        => string.IsNullOrEmpty(entity) || string.Equals(entity, "none", StringComparison.OrdinalIgnoreCase);

    // Attribute lists are sets: order, case and duplicates do not matter.
    private static string? JoinAttributes(IEnumerable<string>? attributes)
        => attributes is null
            ? null
            : string.Join(",", attributes
                .Select(attribute => attribute.Trim())
                .Where(attribute => attribute.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(attribute => attribute, StringComparer.OrdinalIgnoreCase));

    private static void Compare(List<string> differences, string field, string? current, string? desired, bool ignoreCase = false)
    {
        var before = string.IsNullOrEmpty(current) ? null : current;
        var after = string.IsNullOrEmpty(desired) ? null : desired;

        if (!string.Equals(before, after, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
        {
            differences.Add($"{field}: '{before}' -> '{after}'");
        }
    }

    private static string Flag(bool value) => value ? "true" : "false";

    private static string NormalizeVersion(string version)
        => Version.TryParse(version, out var parsed)
            ? new Version(parsed.Major, parsed.Minor, Math.Max(parsed.Build, 0), Math.Max(parsed.Revision, 0)).ToString()
            : version;
}