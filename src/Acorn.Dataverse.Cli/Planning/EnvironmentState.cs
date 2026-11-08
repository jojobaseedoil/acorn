using Acorn.Dataverse.Plugins.Registration;

namespace Acorn.Dataverse.Cli.Planning;

/// <summary>What exists in the environment for one plug-in package.</summary>
internal sealed record EnvironmentState
{
    public PackageState? Package { get; init; }

    public IReadOnlyList<StepState> Steps { get; init; } = [];
}

internal sealed record PackageState(Guid Id, string UniqueName, string Version);

internal sealed record StepState
{
    public Guid Id { get; init; }

    public string TypeName { get; init; } = string.Empty;

    public string MessageName { get; init; } = string.Empty;

    public string? PrimaryEntityName { get; init; }

    public string? SecondaryEntityName { get; init; }

    public PipelineStage Stage { get; init; }

    public ExecutionMode Mode { get; init; }

    public int ExecutionOrder { get; init; }

    public IReadOnlyList<string> FilteringAttributes { get; init; } = [];

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public string? UnsecureConfiguration { get; init; }

    public string? RunAsUser { get; init; }

    public bool AsyncAutoDelete { get; init; }

    public IReadOnlyList<ImageState> Images { get; init; } = [];
}

internal sealed record ImageState
{
    public Guid Id { get; init; }

    public ImageType ImageType { get; init; }

    public string Alias { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public IReadOnlyList<string>? Attributes { get; init; }

    public string MessagePropertyName { get; init; } = string.Empty;
}