namespace Acorn.Dataverse.Plugins.Registration;

/// <summary>Neutral description of a declared entity image.</summary>
public sealed record PluginImageDefinition
{
    /// <summary>Image type.</summary>
    public ImageType ImageType { get; init; }

    /// <summary>Alias used to read the image from the execution context.</summary>
    public string Alias { get; init; } = string.Empty;

    /// <summary>Image name, or <see langword="null"/> to use the alias.</summary>
    public string? Name { get; init; }

    /// <summary>Attributes included in the image, or <see langword="null"/> for all attributes.</summary>
    public IReadOnlyList<string>? Attributes { get; init; }

    /// <summary>Message property that identifies the record, or <see langword="null"/> to resolve from the message.</summary>
    public string? MessagePropertyName { get; init; }

    /// <summary>Binds the image to the step with this Id, or <see langword="null"/> for every step.</summary>
    public string? StepId { get; init; }
}