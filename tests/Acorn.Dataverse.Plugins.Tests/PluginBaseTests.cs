using Microsoft.Xrm.Sdk;
using NSubstitute;

namespace Acorn.Dataverse.Plugins.Tests;

public class PluginBaseTests
{
    private readonly PluginServices _services = new();

    [Fact]
    public void Execute_WrapsUnexpectedException_InInvalidPluginExecutionException()
    {
        var plugin = new TestPlugin(_ => throw new InvalidOperationException("boom"));

        var ex = Assert.Throws<InvalidPluginExecutionException>(() => plugin.Execute(_services.ServiceProvider));

        Assert.IsType<InvalidOperationException>(ex.InnerException);
    }

    [Fact]
    public void Execute_RethrowsInvalidPluginExecutionException_AsSameInstance()
    {
        var original = new InvalidPluginExecutionException("Business rule violated.");
        var plugin = new TestPlugin(_ => throw original);

        var ex = Assert.Throws<InvalidPluginExecutionException>(() => plugin.Execute(_services.ServiceProvider));

        Assert.Same(original, ex);
    }

    [Fact]
    public void Execute_PassesContextWithProvidedExecutionContext()
    {
        ILocalPluginContext? captured = null;
        var plugin = new TestPlugin(context => captured = context);

        plugin.Execute(_services.ServiceProvider);

        Assert.NotNull(captured);
        Assert.Same(_services.ExecutionContext, captured.ExecutionContext);
    }

    [Fact]
    public void Execute_DoesNotCreateOrganizationServices_WhenNotAccessed()
    {
        var plugin = new TestPlugin(_ => { });

        plugin.Execute(_services.ServiceProvider);

        _services.ServiceFactory.DidNotReceiveWithAnyArgs().CreateOrganizationService(default);
    }

    private sealed class TestPlugin(Action<ILocalPluginContext> action) : PluginBase
    {
        protected override void ExecutePlugin(ILocalPluginContext context) => action(context);
    }
}