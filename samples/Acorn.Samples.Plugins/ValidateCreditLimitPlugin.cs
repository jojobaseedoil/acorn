using Acorn.Dataverse.Plugins;
using Acorn.Dataverse.Plugins.Registration;
using Acorn.Samples.Application;
using Acorn.Samples.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xrm.Sdk;

namespace Acorn.Samples.Plugins;

[PluginStep(PipelineStage.PreOperation, MessageNames.Update, Account.EntityLogicalName,
    FilteringAttributes = new[] { "creditlimit" })]
[PluginImage(ImageType.PreImage, "pre", Attributes = new[] { "creditlimit" })]
public sealed class ValidateCreditLimitPlugin(string? unsecure, string? secure) : SamplePluginBase(unsecure, secure)
{
    protected override void ExecutePlugin(ILocalPluginContext context)
    {
        var target = ((Entity)context.ExecutionContext.InputParameters["Target"]).ToEntity<Account>();
        var before = context.ExecutionContext.PreEntityImages["pre"].ToEntity<Account>();

        context.Services.GetRequiredService<CreditLimitService>()
            .ValidateChange(before.CreditLimit, target.CreditLimit);
    }
}