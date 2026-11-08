using Acorn.Dataverse.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xrm.Sdk;
using NSubstitute;

namespace Acorn.Dataverse.Plugins.Tests.DependencyInjection;

public class PluginDependencyInjectionTests
{
    private readonly PluginServices _services = new();

    [Fact]
    public void Execute_ResolvesServicesRegisteredInConfigureServices()
    {
        ScopedDependency? resolved = null;
        var plugin = new DiPlugin(
            services => services.AddScoped<ScopedDependency>(),
            context => resolved = context.Services.GetRequiredService<ScopedDependency>());

        plugin.Execute(_services.ServiceProvider);

        Assert.NotNull(resolved);
    }

    [Fact]
    public void OrganizationService_IsResolvedAsContextUser()
    {
        var userId = Guid.NewGuid();
        var userService = Substitute.For<IOrganizationService>();
        _services.ExecutionContext.UserId.Returns(userId);
        _services.ServiceFactory.CreateOrganizationService(userId).Returns(userService);

        IOrganizationService? resolved = null;
        var plugin = new DiPlugin(
            _ => { },
            context => resolved = context.Services.GetRequiredService<IOrganizationService>());

        plugin.Execute(_services.ServiceProvider);

        Assert.Same(userService, resolved);
    }

    [Fact]
    public void OrganizationServiceProvider_System_UsesSystemUser()
    {
        var systemService = Substitute.For<IOrganizationService>();
        _services.ServiceFactory.CreateOrganizationService(null).Returns(systemService);

        IOrganizationService? resolved = null;
        var plugin = new DiPlugin(
            _ => { },
            context => resolved = context.Services.GetRequiredService<IOrganizationServiceProvider>().System);

        plugin.Execute(_services.ServiceProvider);

        Assert.Same(systemService, resolved);
    }

    [Fact]
    public void ScopedServices_AreNewForEachExecution()
    {
        var instances = new List<ScopedDependency>();
        var plugin = new DiPlugin(
            services => services.AddScoped<ScopedDependency>(),
            context => instances.Add(context.Services.GetRequiredService<ScopedDependency>()));

        plugin.Execute(_services.ServiceProvider);
        plugin.Execute(_services.ServiceProvider);

        Assert.Equal(2, instances.Count);
        Assert.NotSame(instances[0], instances[1]);
    }

    [Fact]
    public void ScopedServices_AreDisposedAfterExecution()
    {
        DisposableDependency? resolved = null;
        var plugin = new DiPlugin(
            services => services.AddScoped<DisposableDependency>(),
            context => resolved = context.Services.GetRequiredService<DisposableDependency>());

        plugin.Execute(_services.ServiceProvider);

        Assert.NotNull(resolved);
        Assert.True(resolved.Disposed);
    }

    [Fact]
    public void Execute_Throws_WhenSingletonCapturesOrganizationService()
    {
        var plugin = new DiPlugin(
            services => services.AddSingleton<CaptiveDependency>(),
            _ => { });

        Assert.Throws<InvalidPluginExecutionException>(() => plugin.Execute(_services.ServiceProvider));
    }

    private sealed class DiPlugin(
        Action<IServiceCollection> configure,
        Action<ILocalPluginContext> execute) : PluginBase
    {
        protected override void ConfigureServices(IServiceCollection services) => configure(services);

        protected override void ExecutePlugin(ILocalPluginContext context) => execute(context);
    }

    private sealed class ScopedDependency
    {
    }

    private sealed class DisposableDependency : IDisposable
    {
        public bool Disposed { get; private set; }

        public void Dispose() => Disposed = true;
    }

    private sealed class CaptiveDependency(IOrganizationService service)
    {
        public IOrganizationService Service { get; } = service;
    }
}