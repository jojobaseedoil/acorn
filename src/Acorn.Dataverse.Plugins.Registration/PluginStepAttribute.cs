using System.Diagnostics.CodeAnalysis;

namespace Acorn.Dataverse.Plugins.Registration;

/// <summary>
/// Declares a step in which the plug-in is registered. The declaration is the source of truth
/// for deployment and is checked at runtime.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class PluginStepAttribute : Attribute
{
    /// <summary>Declares a plug-in step.</summary>
    /// <param name="stage">Pipeline stage.</param>
    /// <param name="messageName">Message name, e.g. <see cref="MessageNames.Update"/>.</param>
    /// <param name="primaryEntityName">Logical name of the primary entity, or <see langword="null"/> for all entities.</param>
    public PluginStepAttribute(PipelineStage stage, string messageName, string? primaryEntityName = null)
    {
        Stage = stage;
        MessageName = messageName;
        PrimaryEntityName = primaryEntityName;
    }

    /// <summary>Pipeline stage.</summary>
    public PipelineStage Stage { get; }

    /// <summary>Message name.</summary>
    public string MessageName { get; }

    /// <summary>Logical name of the primary entity, or <see langword="null"/> for all entities.</summary>
    public string? PrimaryEntityName { get; }

    /// <summary>Logical name of the secondary entity, for messages that involve two entities.</summary>
    public string? SecondaryEntityName { get; set; }

    /// <summary>Execution mode. Defaults to <see cref="ExecutionMode.Synchronous"/>.</summary>
    public ExecutionMode Mode { get; set; } = ExecutionMode.Synchronous;

    /// <summary>Execution order among steps of the same event. Defaults to 1.</summary>
    public int ExecutionOrder { get; set; } = 1;

    /// <summary>Attributes that trigger the step. Update message only.</summary>
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Attribute arguments only support arrays.")]
    public string[]? FilteringAttributes { get; set; }

    /// <summary>Identifier used by <see cref="PluginImageAttribute.StepId"/> to bind images to this step.</summary>
    public string? Id { get; set; }

    /// <summary>Step name. Generated at deployment when not set.</summary>
    public string? Name { get; set; }

    /// <summary>Step description.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// User the step runs as (domain name / UPN), resolved at deployment.
    /// <see langword="null"/> runs as the calling user.
    /// </summary>
    public string? RunAsUser { get; set; }

    /// <summary>Deletes the async operation when it succeeds. Asynchronous steps only.</summary>
    public bool AsyncAutoDelete { get; set; }

    /// <summary>
    /// Unsecure configuration. Never put secrets here: secure configuration is defined
    /// in the environment, not in source code.
    /// </summary>
    public string? UnsecureConfiguration { get; set; }
}