using System.Diagnostics.CodeAnalysis;
using Microsoft.Xrm.Sdk;

namespace Acorn.Dataverse.Plugins;

/// <summary>Typed access to the target, images and parameters of the current execution.</summary>
public static class PluginContextExtensions
{
    private const string TargetParameterName = "Target";

    /// <summary>
    /// Returns the <c>Target</c> entity as <typeparamref name="T"/>. The result shares its attributes with the
    /// pipeline target: in PreValidation and PreOperation, changes made to it are saved by the operation.
    /// </summary>
    /// <exception cref="InvalidPluginExecutionException">The target is missing or is not an entity.</exception>
    public static T GetTarget<T>(this ILocalPluginContext context) where T : Entity
    {
        var executionContext = ExecutionOf(context);
        return TryGetTargetCore<T>(executionContext, out var target)
            ? target
            : throw new InvalidPluginExecutionException(
                $"The '{TargetParameterName}' input parameter is missing or is not an entity (message '{executionContext.MessageName}').");
    }

    /// <summary>Tries to return the <c>Target</c> entity as <typeparamref name="T"/>.</summary>
    public static bool TryGetTarget<T>(this ILocalPluginContext context, [NotNullWhen(true)] out T? target) where T : Entity
        => TryGetTargetCore(ExecutionOf(context), out target);

    /// <summary>
    /// Returns a reference to the target record. Works when <c>Target</c> is an entity (Create, Update)
    /// or an entity reference (Delete, Assign, SetState).
    /// </summary>
    /// <exception cref="InvalidPluginExecutionException">The target is missing.</exception>
    public static EntityReference GetTargetReference(this ILocalPluginContext context)
    {
        var executionContext = ExecutionOf(context);
        var value = executionContext.InputParameters.TryGetValue(TargetParameterName, out var raw) ? raw : null;

        return value switch
        {
            EntityReference reference => reference,
            Entity entity => new EntityReference(entity.LogicalName, IdOf(entity, executionContext)),
            _ => throw new InvalidPluginExecutionException(
                $"The '{TargetParameterName}' input parameter is missing (message '{executionContext.MessageName}')."),
        };
    }

    /// <summary>Returns the pre-image registered with <paramref name="alias"/> as <typeparamref name="T"/>.</summary>
    /// <exception cref="InvalidPluginExecutionException">The image is not available.</exception>
    public static T GetPreImage<T>(this ILocalPluginContext context, string alias) where T : Entity
        => GetImage<T>(ExecutionOf(context).PreEntityImages, alias, "Pre-image");

    /// <summary>Tries to return the pre-image registered with <paramref name="alias"/>.</summary>
    public static bool TryGetPreImage<T>(this ILocalPluginContext context, string alias, [NotNullWhen(true)] out T? image)
        where T : Entity
        => TryGetImage(ExecutionOf(context).PreEntityImages, alias, out image);

    /// <summary>Returns the post-image registered with <paramref name="alias"/> as <typeparamref name="T"/>.</summary>
    /// <exception cref="InvalidPluginExecutionException">The image is not available.</exception>
    public static T GetPostImage<T>(this ILocalPluginContext context, string alias) where T : Entity
        => GetImage<T>(ExecutionOf(context).PostEntityImages, alias, "Post-image");

    /// <summary>Tries to return the post-image registered with <paramref name="alias"/>.</summary>
    public static bool TryGetPostImage<T>(this ILocalPluginContext context, string alias, [NotNullWhen(true)] out T? image)
        where T : Entity
        => TryGetImage(ExecutionOf(context).PostEntityImages, alias, out image);

    /// <summary>
    /// Returns the record as it will be after the operation: the pre-image overlaid with the target.
    /// Target attributes win, including attributes cleared to <see langword="null"/>.
    /// </summary>
    /// <remarks>Returns a new entity: changes made to it do not affect the operation.</remarks>
    /// <exception cref="InvalidPluginExecutionException">The target or the pre-image is not available.</exception>
    public static T GetMergedTarget<T>(this ILocalPluginContext context, string preImageAlias) where T : Entity
    {
        var executionContext = ExecutionOf(context);
        var target = GetTarget<Entity>(context);
        var preImage = GetPreImage<Entity>(context, preImageAlias);

        var merged = new Entity(target.LogicalName, IdOf(target, executionContext));
        foreach (var attribute in preImage.Attributes)
        {
            merged[attribute.Key] = attribute.Value;
        }

        foreach (var attribute in target.Attributes)
        {
            merged[attribute.Key] = attribute.Value;
        }

        return merged.ToEntity<T>();
    }

    /// <summary>Returns the input parameter <paramref name="name"/> as <typeparamref name="T"/>.</summary>
    /// <exception cref="InvalidPluginExecutionException">The parameter is missing or has another type.</exception>
    public static T GetInputParameter<T>(this ILocalPluginContext context, string name)
    {
        var executionContext = ExecutionOf(context);
        if (!executionContext.InputParameters.TryGetValue(name, out var value))
        {
            throw new InvalidPluginExecutionException($"Input parameter '{name}' is missing.");
        }

        return value is T typed
            ? typed
            : throw new InvalidPluginExecutionException(
                $"Input parameter '{name}' is {value?.GetType().Name ?? "null"}, expected {typeof(T).Name}.");
    }

    /// <summary>Tries to return the input parameter <paramref name="name"/> as <typeparamref name="T"/>.</summary>
    public static bool TryGetInputParameter<T>(this ILocalPluginContext context, string name, [MaybeNullWhen(false)] out T value)
    {
        if (ExecutionOf(context).InputParameters.TryGetValue(name, out var raw) && raw is T typed)
        {
            value = typed;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>Sets the output parameter <paramref name="name"/> (Custom APIs).</summary>
    public static void SetOutputParameter(this ILocalPluginContext context, string name, object? value)
        => ExecutionOf(context).OutputParameters[name] = value;

    private static IPluginExecutionContext ExecutionOf(ILocalPluginContext context)
        => context?.ExecutionContext ?? throw new ArgumentNullException(nameof(context));

    private static bool TryGetTargetCore<T>(IPluginExecutionContext executionContext, [NotNullWhen(true)] out T? target)
        where T : Entity
    {
        target = executionContext.InputParameters.TryGetValue(TargetParameterName, out var value) && value is Entity entity
            ? entity.ToEntity<T>()
            : null;
        return target is not null;
    }

    private static T GetImage<T>(EntityImageCollection images, string alias, string kind) where T : Entity
        => TryGetImage<T>(images, alias, out var image)
            ? image
            : throw new InvalidPluginExecutionException(
                $"{kind} '{alias}' is not available. Check the [PluginImage] declaration and the step registration.");

    private static bool TryGetImage<T>(EntityImageCollection images, string alias, [NotNullWhen(true)] out T? image)
        where T : Entity
    {
        image = images.TryGetValue(alias, out var entity) && entity is not null ? entity.ToEntity<T>() : null;
        return image is not null;
    }

    private static Guid IdOf(Entity entity, IPluginExecutionContext executionContext)
        => entity.Id != Guid.Empty ? entity.Id : executionContext.PrimaryEntityId;
}