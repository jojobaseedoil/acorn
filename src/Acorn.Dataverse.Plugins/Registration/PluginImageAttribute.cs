using System.Diagnostics.CodeAnalysis;

namespace Acorn.Dataverse.Plugins.Registration;

/// <summary>
/// Declares an entity image. Without <see cref="StepId"/>, applies to every step declared on the class.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class PluginImageAttribute : Attribute
{
    /// <summary>Declares an entity image.</summary>
    /// <param name="imageType">Image type.</param>
    /// <param name="alias">Alias used to read the image from the execution context.</param>
    public PluginImageAttribute(ImageType imageType, string alias)
    {
        ImageType = imageType;
        Alias = alias;
    }

    /// <summary>Image type.</summary>
    public ImageType ImageType { get; }

    /// <summary>Alias used to read the image from the execution context.</summary>
    public string Alias { get; }

    /// <summary>Image name. Defaults to the alias at deployment.</summary>
    public string? Name { get; set; }

    /// <summary>Attributes included in the image. <see langword="null"/> includes all attributes (avoid: costly).</summary>
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Attribute arguments only support arrays.")]
    public string[]? Attributes { get; set; }

    /// <summary>Message property that identifies the record. Resolved from the message at deployment when not set.</summary>
    public string? MessagePropertyName { get; set; }

    /// <summary>Binds the image to the step with this <see cref="PluginStepAttribute.Id"/>.</summary>
    public string? StepId { get; set; }
}