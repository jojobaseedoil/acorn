using Microsoft.Xrm.Sdk;
using NSubstitute;

namespace Acorn.Dataverse.Plugins.Tests;

/// <summary>
/// Substitutes for the services Dataverse provides to a plug-in, already wired into a service provider.
/// </summary>
internal sealed class PluginServices
{
    public PluginServices()
    {
        ServiceProvider.GetService(typeof(IPluginExecutionContext)).Returns(ExecutionContext);
        ServiceProvider.GetService(typeof(ITracingService)).Returns(TracingService);
        ServiceProvider.GetService(typeof(IOrganizationServiceFactory)).Returns(ServiceFactory);
    }

    public IServiceProvider ServiceProvider { get; } = Substitute.For<IServiceProvider>();

    public IPluginExecutionContext ExecutionContext { get; } = Substitute.For<IPluginExecutionContext>();

    public ITracingService TracingService { get; } = Substitute.For<ITracingService>();

    public IOrganizationServiceFactory ServiceFactory { get; } = Substitute.For<IOrganizationServiceFactory>();
}