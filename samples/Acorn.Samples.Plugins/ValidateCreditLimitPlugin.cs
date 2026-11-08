using Acorn.Dataverse.Plugins;
using Acorn.Dataverse.Plugins.Registration;
using Acorn.Samples.Application;
using Acorn.Samples.Model;
using Microsoft.Extensions.DependencyInjection;

namespace Acorn.Samples.Plugins;

[PluginStep(PipelineStage.PreOperation, MessageNames.Update, Account.EntityLogicalName,
    FilteringAttributes = new[] { "creditlimit" },
    Description = "Blocks credit limit increases above 50%.")]
[PluginImage(ImageType.PreImage, "pre", Attributes = new[] { "creditlimit" })]
public sealed class ValidateCreditLimitPlugin(string? unsecure, string? secure) : SamplePluginBase(unsecure, secure)
{
    protected override void ExecutePlugin(ILocalPluginContext context)
    {
        var target = context.GetTarget<Account>();
        var before = context.GetPreImage<Account>("pre");

        context.Services.GetRequiredService<CreditLimitService>()
            .ValidateChange(before.CreditLimit, target.CreditLimit);
    }
}