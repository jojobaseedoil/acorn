namespace Acorn.Dataverse.Plugins.Registration;

/// <summary>Neutral description of a declared plug-in step, independent of how it was read.</summary>
public sealed record PluginStepDefinition
{
    /// <summary>Pipeline stage.</summary>
    public PipelineStage Stage { get; init; }

    /// <summary>Message name.</summary>
    public string MessageName { get; init; } = string.Empty;

    /// <summary>Logical name of the primary entity, or <see langword="null"/> for all entities.</summary>
    public string? PrimaryEntityName { get; init; }

    /// <summary>Logical name of the secondary entity.</summary>
    public string? SecondaryEntityName { get; init; }

    /// <summary>Execution mode.</summary>
    public ExecutionMode Mode { get; init; }

    /// <summary>Execution order.</summary>
    public int ExecutionOrder { get; init; } = 1;

    /// <summary>Attributes that trigger the step. Empty means all attributes.</summary>
    public IReadOnlyList<string> FilteringAttributes { get; init; } = Array.Empty<string>();

    /// <summary>Identifier used to bind images to this step.</summary>
    public string? Id { get; init; }

    /// <summary>Step name, or <see langword="null"/> to generate one.</summary>
    public string? Name { get; init; }

    /// <summary>Step description.</summary>
    public string? Description { get; init; }

    /// <summary>User the step runs as (domain name / UPN), or <see langword="null"/> for the calling user.</summary>
    public string? RunAsUser { get; init; }

    /// <summary>Deletes the async operation when it succeeds.</summary>
    public bool AsyncAutoDelete { get; init; }

    /// <summary>Unsecure configuration.</summary>
    public string? UnsecureConfiguration { get; init; }
}