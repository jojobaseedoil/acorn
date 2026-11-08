using Microsoft.Xrm.Sdk;
using NSubstitute;

namespace Acorn.Dataverse.Plugins.Tests;

public class LocalPluginContextTests
{
    private readonly PluginServices _services = new();

    [Fact]
    public void OrganizationService_UsesContextUserId()
    {
        var userId = Guid.NewGuid();
        _services.ExecutionContext.UserId.Returns(userId);
        var context = new LocalPluginContext(_services.ServiceProvider);

        _ = context.OrganizationService;

        _services.ServiceFactory.Received(1).CreateOrganizationService(userId);
    }

    [Fact]
    public void InitiatingUserService_UsesInitiatingUserId()
    {
        var initiatingUserId = Guid.NewGuid();
        _services.ExecutionContext.InitiatingUserId.Returns(initiatingUserId);
        var context = new LocalPluginContext(_services.ServiceProvider);

        _ = context.InitiatingUserService;

        _services.ServiceFactory.Received(1).CreateOrganizationService(initiatingUserId);
    }

    [Fact]
    public void SystemService_UsesNullUserId()
    {
        var context = new LocalPluginContext(_services.ServiceProvider);

        _ = context.SystemService;

        _services.ServiceFactory.Received(1).CreateOrganizationService(null);
    }

    [Fact]
    public void OrganizationService_IsCreatedOnlyOnce()
    {
        var context = new LocalPluginContext(_services.ServiceProvider);

        _ = context.OrganizationService;
        _ = context.OrganizationService;

        _services.ServiceFactory.ReceivedWithAnyArgs(1).CreateOrganizationService(default);
    }

    [Fact]
    public void Trace_DoesNotTreatMessageAsFormatString()
    {
        var context = new LocalPluginContext(_services.ServiceProvider);
        const string message = "{\"name\": \"Contoso\"}";

        context.Trace(message);

        _services.TracingService.Received(1).Trace("{0}", message);
    }

    [Fact]
    public void Constructor_Throws_WhenRequiredServiceIsMissing()
    {
        _services.ServiceProvider.GetService(typeof(ITracingService)).Returns((object?)null);

        var ex = Assert.Throws<InvalidPluginExecutionException>(
            () => new LocalPluginContext(_services.ServiceProvider));

        Assert.Contains(nameof(ITracingService), ex.Message);
    }
}