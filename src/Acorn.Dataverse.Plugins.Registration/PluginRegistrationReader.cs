using System.Reflection;

namespace Acorn.Dataverse.Plugins.Registration;

/// <summary>
/// Reads <see cref="PluginStepAttribute"/> and <see cref="PluginImageAttribute"/> declarations from loaded types
/// into the neutral definition model.
/// </summary>
public static class PluginRegistrationReader
{
    internal const string PluginInterfaceName = "Microsoft.Xrm.Sdk.IPlugin";

    /// <summary>Reads every concrete plug-in type in the assembly.</summary>
    public static IReadOnlyList<PluginTypeDefinition> Read(Assembly assembly)
    {
        Guard.NotNull(assembly);

        return assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && ImplementsIPlugin(t))
            .Select(t => Read(t))
            .ToList();
    }

    /// <summary>Reads the declarations of a type.</summary>
    public static PluginTypeDefinition Read(Type type)
    {
        Guard.NotNull(type);

        return new PluginTypeDefinition
        {
            TypeName = type.FullName ?? type.Name,
            ImplementsPlugin = ImplementsIPlugin(type),
            Steps = type.GetCustomAttributes<PluginStepAttribute>(inherit: false).Select(ToStepDefinition).ToList(),
            Images = type.GetCustomAttributes<PluginImageAttribute>(inherit: false).Select(ToImageDefinition).ToList(),
        };
    }

    // Matches by name: keeps this assembly free of the Dataverse SDK and also works with MetadataLoadContext.
    internal static bool ImplementsIPlugin(Type type) => type.GetInterface(PluginInterfaceName) is not null;

    private static PluginStepDefinition ToStepDefinition(PluginStepAttribute attribute) => new()
    {
        Stage = attribute.Stage,
        MessageName = attribute.MessageName,
        PrimaryEntityName = attribute.PrimaryEntityName,
        SecondaryEntityName = attribute.SecondaryEntityName,
        Mode = attribute.Mode,
        ExecutionOrder = attribute.ExecutionOrder,
        FilteringAttributes = attribute.FilteringAttributes ?? Array.Empty<string>(),
        Id = attribute.Id,
        Name = attribute.Name,
        Description = attribute.Description,
        RunAsUser = attribute.RunAsUser,
        AsyncAutoDelete = attribute.AsyncAutoDelete,
        UnsecureConfiguration = attribute.UnsecureConfiguration,
    };

    private static PluginImageDefinition ToImageDefinition(PluginImageAttribute attribute) => new()
    {
        ImageType = attribute.ImageType,
        Alias = attribute.Alias,
        Name = attribute.Name,
        Attributes = attribute.Attributes,
        MessagePropertyName = attribute.MessagePropertyName,
        StepId = attribute.StepId,
    };
}