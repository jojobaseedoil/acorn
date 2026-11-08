using Acorn.Dataverse.Plugins.Registration;
using Microsoft.Xrm.Sdk;
using NSubstitute;

namespace Acorn.Dataverse.Plugins.Tests.Registration;

public class PluginStepRuntimeCheckTests
{
    private readonly PluginServices _services = new();

    [Fact]
    public void Execute_Runs_WhenContextMatchesDeclaredStep()
    {
        GivenExecution(PipelineStage.PostOperation, MessageNames.Update, "account", ExecutionMode.Synchronous);
        var plugin = new AccountUpdatePlugin();

        plugin.Execute(_services.ServiceProvider);

        Assert.True(plugin.Executed);
    }

    [Fact]
    public void Execute_MatchesMessageAndEntity_IgnoringCase()
    {
        GivenExecution(PipelineStage.PostOperation, "update", "Account", ExecutionMode.Synchronous);
        var plugin = new AccountUpdatePlugin();

        plugin.Execute(_services.ServiceProvider);

        Assert.True(plugin.Executed);
    }

    [Fact]
    public void Execute_Throws_WhenMessageIsNotDeclared()
    {
        GivenExecution(PipelineStage.PostOperation, MessageNames.Create, "account", ExecutionMode.Synchronous);
        var plugin = new AccountUpdatePlugin();

        var ex = Assert.Throws<InvalidPluginExecutionException>(() => plugin.Execute(_services.ServiceProvider));

        Assert.Contains("is not declared", ex.Message);
        Assert.False(plugin.Executed);
    }

    [Fact]
    public void Execute_Throws_WhenModeDiffers()
    {
        GivenExecution(PipelineStage.PostOperation, MessageNames.Update, "account", ExecutionMode.Asynchronous);
        var plugin = new AccountUpdatePlugin();

        Assert.Throws<InvalidPluginExecutionException>(() => plugin.Execute(_services.ServiceProvider));
    }

    [Fact]
    public void Execute_Runs_ForAnyEntity_WhenStepHasNoEntity()
    {
        GivenExecution(PipelineStage.PostOperation, MessageNames.Create, "contact", ExecutionMode.Synchronous);
        var plugin = new AnyEntityCreatePlugin();

        plugin.Execute(_services.ServiceProvider);

        Assert.True(plugin.Executed);
    }

    private void GivenExecution(PipelineStage stage, string messageName, string entityName, ExecutionMode mode)
    {
        _services.ExecutionContext.Stage.Returns((int)stage);
        _services.ExecutionContext.MessageName.Returns(messageName);
        _services.ExecutionContext.PrimaryEntityName.Returns(entityName);
        _services.ExecutionContext.Mode.Returns((int)mode);
    }

    [PluginStep(PipelineStage.PostOperation, MessageNames.Update, "account")]
    private sealed class AccountUpdatePlugin : PluginBase
    {
        public bool Executed { get; private set; }

        protected override void ExecutePlugin(ILocalPluginContext context) => Executed = true;
    }

    [PluginStep(PipelineStage.PostOperation, MessageNames.Create)]
    private sealed class AnyEntityCreatePlugin : PluginBase
    {
        public bool Executed { get; private set; }

        protected override void ExecutePlugin(ILocalPluginContext context) => Executed = true;
    }
}