using Acorn.Dataverse.Plugins;
using Acorn.Dataverse.Plugins.Registration;
using Acorn.Samples.Application;
using Acorn.Samples.Model;
using Microsoft.Extensions.DependencyInjection;

namespace Acorn.Samples.Plugins;

[PluginStep(PipelineStage.PostOperation, MessageNames.Update, Account.EntityLogicalName,
    FilteringAttributes = new[] { "creditlimit" })]
[PluginImage(ImageType.PreImage, "pre", Attributes = new[] { "creditlimit" })]
[PluginImage(ImageType.PostImage, "post", Attributes = new[] { "creditlimit" })]
public sealed class LogCreditLimitChangePlugin(string? unsecure, string? secure) : SamplePluginBase(unsecure, secure)
{
    protected override void ExecutePlugin(ILocalPluginContext context)
    {
        var executionContext = context.ExecutionContext;
        var before = executionContext.PreEntityImages["pre"].ToEntity<Account>();
        var after = executionContext.PostEntityImages["post"].ToEntity<Account>();

        context.Services.GetRequiredService<CreditLimitService>()
            .LogChange(executionContext.PrimaryEntityId, before.CreditLimit, after.CreditLimit);
    }
}