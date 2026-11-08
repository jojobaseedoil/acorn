using System.Reflection;

namespace Acorn.Dataverse.Plugins.Registration;

/// <summary>
/// Validates <see cref="PluginStepAttribute"/> and <see cref="PluginImageAttribute"/> declarations
/// against Dataverse registration rules. Call it from a unit test in each plug-in project.
/// </summary>
public static class PluginStepValidator
{
    private const string PluginInterfaceName = "Microsoft.Xrm.Sdk.IPlugin";

    private static readonly HashSet<string> MessagesWithImages = new(StringComparer.OrdinalIgnoreCase)
    {
        MessageNames.Assign, MessageNames.Create, MessageNames.Delete, "DeliverIncoming", "DeliverPromote",
        MessageNames.Merge, "Route", "Send", MessageNames.SetState, MessageNames.Update,
    };

    /// <summary>Validates every concrete plug-in type in the assembly. Throws if any declaration is invalid.</summary>
    public static void EnsureValid(Assembly assembly)
    {
        if (assembly is null)
        {
            throw new ArgumentNullException(nameof(assembly));
        }

        var errors = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && ImplementsIPlugin(t))
            .SelectMany(t => Validate(t))
            .ToList();

        ThrowIfAny(errors);
    }

    /// <summary>Validates a plug-in type. Throws if any declaration is invalid.</summary>
    public static void EnsureValid(Type pluginType) => ThrowIfAny(Validate(pluginType));

    /// <summary>Validates a plug-in type and returns the errors found.</summary>
    public static IReadOnlyList<string> Validate(Type pluginType)
    {
        if (pluginType is null)
        {
            throw new ArgumentNullException(nameof(pluginType));
        }

        var errors = new List<string>();
        var steps = pluginType.GetCustomAttributes<PluginStepAttribute>(inherit: false).ToArray();
        var images = pluginType.GetCustomAttributes<PluginImageAttribute>(inherit: false).ToArray();

        if (steps.Length == 0)
        {
            if (images.Length > 0)
            {
                errors.Add($"{pluginType.FullName}: [PluginImage] declared without any [PluginStep].");
            }

            return errors;
        }

        if (!ImplementsIPlugin(pluginType))
        {
            errors.Add($"{pluginType.FullName}: [PluginStep] can only be applied to types that implement {PluginInterfaceName}.");
        }

        foreach (var step in steps)
        {
            ValidateStep(pluginType, step, errors);
        }

        ValidateDuplicateSteps(pluginType, steps, errors);

        foreach (var image in images)
        {
            ValidateImage(pluginType, image, steps, errors);
        }

        ValidateDuplicateAliases(pluginType, steps, images, errors);

        return errors;
    }

    private static void ValidateStep(Type type, PluginStepAttribute step, List<string> errors)
    {
        var prefix = $"{type.FullName} [{Describe(step)}]";

        if (string.IsNullOrWhiteSpace(step.MessageName))
        {
            errors.Add($"{prefix}: message name is required.");
        }

        if (!Enum.IsDefined(typeof(PipelineStage), step.Stage))
        {
            errors.Add($"{prefix}: {(int)step.Stage} is not a valid pipeline stage.");
        }
        else if (step.Stage == PipelineStage.MainOperation)
        {
            errors.Add($"{prefix}: MainOperation is reserved for Custom API plug-ins, which are not registered as steps.");
        }

        if (step.Mode == ExecutionMode.Asynchronous && step.Stage != PipelineStage.PostOperation)
        {
            errors.Add($"{prefix}: asynchronous steps must be registered in PostOperation.");
        }

        if (step.AsyncAutoDelete && step.Mode != ExecutionMode.Asynchronous)
        {
            errors.Add($"{prefix}: AsyncAutoDelete applies only to asynchronous steps.");
        }

        if (step.FilteringAttributes is { Length: > 0 } && !IsMessage(step, MessageNames.Update))
        {
            errors.Add($"{prefix}: FilteringAttributes are supported only on the Update message.");
        }

        if (step.FilteringAttributes?.Any(string.IsNullOrWhiteSpace) == true)
        {
            errors.Add($"{prefix}: FilteringAttributes contains an empty attribute name.");
        }
    }

    private static void ValidateDuplicateSteps(Type type, PluginStepAttribute[] steps, List<string> errors)
    {
        var duplicatedSteps = steps
            .GroupBy(s => $"{(int)s.Stage}|{(int)s.Mode}|{s.MessageName}|{s.PrimaryEntityName}", StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1);

        foreach (var group in duplicatedSteps)
        {
            errors.Add($"{type.FullName} [{Describe(group.First())}]: step declared more than once.");
        }

        var duplicatedIds = steps
            .Where(s => s.Id is not null)
            .GroupBy(s => s.Id!, StringComparer.Ordinal)
            .Where(g => g.Count() > 1);

        foreach (var group in duplicatedIds)
        {
            errors.Add($"{type.FullName}: step Id '{group.Key}' is used more than once.");
        }
    }

    private static void ValidateImage(Type type, PluginImageAttribute image, PluginStepAttribute[] steps, List<string> errors)
    {
        var prefix = $"{type.FullName} [image '{image.Alias}']";

        if (string.IsNullOrWhiteSpace(image.Alias))
        {
            errors.Add($"{prefix}: alias is required.");
        }

        if (image.Attributes?.Any(string.IsNullOrWhiteSpace) == true)
        {
            errors.Add($"{prefix}: Attributes contains an empty attribute name.");
        }

        var targets = steps.Where(s => Targets(image, s)).ToList();
        if (image.StepId is not null && targets.Count == 0)
        {
            errors.Add($"{prefix}: StepId '{image.StepId}' does not match any [PluginStep] Id.");
            return;
        }

        var hint = image.StepId is null && steps.Length > 1
            ? " Use StepId to bind the image to specific steps."
            : string.Empty;
        var wantsPre = image.ImageType is ImageType.PreImage or ImageType.Both;
        var wantsPost = image.ImageType is ImageType.PostImage or ImageType.Both;

        foreach (var step in targets)
        {
            if (string.IsNullOrWhiteSpace(step.MessageName))
            {
                continue;
            }

            var stepPrefix = $"{prefix} on [{Describe(step)}]";

            if (!MessagesWithImages.Contains(step.MessageName))
            {
                errors.Add($"{stepPrefix}: message '{step.MessageName}' does not support images.{hint}");
                continue;
            }

            if (wantsPre && IsMessage(step, MessageNames.Create))
            {
                errors.Add($"{stepPrefix}: pre-images are not available on Create.{hint}");
            }

            if (wantsPost && IsMessage(step, MessageNames.Delete))
            {
                errors.Add($"{stepPrefix}: post-images are not available on Delete.{hint}");
            }

            if (wantsPost && step.Stage != PipelineStage.PostOperation)
            {
                errors.Add($"{stepPrefix}: post-images are available only in PostOperation.{hint}");
            }
        }
    }

    private static void ValidateDuplicateAliases(
        Type type, PluginStepAttribute[] steps, PluginImageAttribute[] images, List<string> errors)
    {
        foreach (var step in steps)
        {
            var duplicatedAliases = images
                .Where(i => Targets(i, step) && !string.IsNullOrWhiteSpace(i.Alias))
                .GroupBy(i => i.Alias, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1);

            foreach (var group in duplicatedAliases)
            {
                errors.Add($"{type.FullName} [{Describe(step)}]: image alias '{group.Key}' is used more than once.");
            }
        }
    }

    // Matches by name instead of typeof(IPlugin): keeps this namespace free of the Dataverse SDK
    // and also works with types loaded through MetadataLoadContext.
    private static bool ImplementsIPlugin(Type type) =>
        type.GetInterface(PluginInterfaceName) is not null;

    private static bool Targets(PluginImageAttribute image, PluginStepAttribute step) =>
        image.StepId is null || string.Equals(image.StepId, step.Id, StringComparison.Ordinal);

    private static bool IsMessage(PluginStepAttribute step, string messageName) =>
        string.Equals(step.MessageName, messageName, StringComparison.OrdinalIgnoreCase);

    private static string Describe(PluginStepAttribute step) =>
        $"{step.Stage} {step.MessageName} of '{step.PrimaryEntityName ?? "any entity"}'" +
        (step.Mode == ExecutionMode.Asynchronous ? " (async)" : string.Empty);

    private static void ThrowIfAny(IReadOnlyCollection<string> errors)
    {
        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                "Invalid plug-in step declarations:" + Environment.NewLine + string.Join(Environment.NewLine, errors));
        }
    }
}