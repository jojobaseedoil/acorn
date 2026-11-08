using Acorn.Dataverse.Plugins;
using Acorn.Dataverse.Plugins.Registration;

namespace Acorn.Dataverse.Cli.Tests.Fixture;

/// <summary>Sets every property of both attributes, so the metadata reader must map all of them.</summary>
[PluginStep(PipelineStage.PostOperation, MessageNames.Update, "account",
    SecondaryEntityName = "contact",
    Mode = ExecutionMode.Asynchronous,
    ExecutionOrder = 5,
    FilteringAttributes = new[] { "name", "creditlimit" },
    Id = "update",
    Name = "Fully declared step",
    Description = "Every property is set",
    RunAsUser = "admin@contoso.com",
    AsyncAutoDelete = true,
    UnsecureConfiguration = "{\"key\":\"value\"}")]
[PluginImage(ImageType.Both, "snapshot",
    Name = "Snapshot",
    Attributes = new[] { "name" },
    MessagePropertyName = "Target",
    StepId = "update")]
public sealed class FullyDeclaredPlugin : PluginBase
{
    protected override void ExecutePlugin(ILocalPluginContext context)
    {
    }
}

/// <summary>Sets nothing optional, so the reader must apply the attribute defaults.</summary>
[PluginStep(PipelineStage.PostOperation, MessageNames.Create, "contact")]
[PluginImage(ImageType.PostImage, "post")]
public sealed class MinimalPlugin : PluginBase
{
    protected override void ExecutePlugin(ILocalPluginContext context)
    {
    }
}

/// <summary>Abstract: must be ignored even with a declaration.</summary>
[PluginStep(PipelineStage.PreOperation, MessageNames.Delete, "account")]
public abstract class AbstractPlugin : PluginBase
{
}

/// <summary>Does not implement IPlugin: must be ignored even with a declaration.</summary>
[PluginStep(PipelineStage.PreOperation, MessageNames.Delete, "account")]
public sealed class NotAPlugin
{
}