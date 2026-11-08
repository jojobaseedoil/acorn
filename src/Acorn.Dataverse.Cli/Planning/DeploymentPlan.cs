using Acorn.Dataverse.Plugins.Registration;

namespace Acorn.Dataverse.Cli.Planning;

/// <summary>What the plug-in package declares.</summary>
internal sealed record DesiredState(
    string PackageUniqueName,
    string PackageVersion,
    IReadOnlyList<PluginTypeDefinition> PluginTypes);

internal enum ChangeKind
{
    Unchanged,
    Create,
    Update,
    Delete,

    /// <summary>Exists in the environment but is not declared; kept because --prune was not used.</summary>
    Orphan,
}

internal sealed record DeploymentPlan(
    PackageChange Package,
    IReadOnlyList<StepChange> Steps,
    IReadOnlyList<string> Errors)
{
    public bool HasErrors => Errors.Count > 0;
}

internal sealed record PackageChange(ChangeKind Kind, string UniqueName, string Version);

internal sealed record StepChange(
    ChangeKind Kind,
    string TypeName,
    string DisplayName,
    PluginStepDefinition? Desired,
    StepState? Current,
    IReadOnlyList<string> Differences,
    IReadOnlyList<ImageChange> Images);

internal sealed record ImageChange(
    ChangeKind Kind,
    string Alias,
    PluginImageDefinition? Desired,
    ImageState? Current,
    IReadOnlyList<string> Differences);