namespace Acorn.Dataverse.Plugins.Registration;

/// <summary>Neutral description of a plug-in type and its declared steps and images.</summary>
public sealed record PluginTypeDefinition
{
    /// <summary>Full name of the type.</summary>
    public string TypeName { get; init; } = string.Empty;

    /// <summary>Whether the type implements <c>Microsoft.Xrm.Sdk.IPlugin</c>.</summary>
    public bool ImplementsPlugin { get; init; }

    /// <summary>Declared steps.</summary>
    public IReadOnlyList<PluginStepDefinition> Steps { get; init; } = Array.Empty<PluginStepDefinition>();

    /// <summary>Declared images.</summary>
    public IReadOnlyList<PluginImageDefinition> Images { get; init; } = Array.Empty<PluginImageDefinition>();
}