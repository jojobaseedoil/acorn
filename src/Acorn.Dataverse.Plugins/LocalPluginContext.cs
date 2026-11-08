using Microsoft.Xrm.Sdk;

namespace Acorn.Dataverse.Plugins;

/// <summary>
/// Default <see cref="ILocalPluginContext"/> implementation built from the Dataverse service provider.
/// </summary>
public sealed class LocalPluginContext : ILocalPluginContext
{
    private readonly Lazy<IOrganizationService> _organizationService;
    private readonly Lazy<IOrganizationService> _initiatingUserService;
    private readonly Lazy<IOrganizationService> _systemService;

    /// <summary>Creates the context from the service provider supplied by Dataverse.</summary>
    public LocalPluginContext(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        ExecutionContext = GetRequiredService<IPluginExecutionContext>(serviceProvider);
        TracingService = GetRequiredService<ITracingService>(serviceProvider);

        var factory = GetRequiredService<IOrganizationServiceFactory>(serviceProvider);
        _organizationService = new Lazy<IOrganizationService>(() => factory.CreateOrganizationService(ExecutionContext.UserId));
        _initiatingUserService = new Lazy<IOrganizationService>(() => factory.CreateOrganizationService(ExecutionContext.InitiatingUserId));
        _systemService = new Lazy<IOrganizationService>(() => factory.CreateOrganizationService(null));
    }

    /// <inheritdoc />
    public IServiceProvider ServiceProvider { get; }

    /// <inheritdoc />
    public IPluginExecutionContext ExecutionContext { get; }

    /// <inheritdoc />
    public ITracingService TracingService { get; }

    /// <inheritdoc />
    public IOrganizationService OrganizationService => _organizationService.Value;

    /// <inheritdoc />
    public IOrganizationService InitiatingUserService => _initiatingUserService.Value;

    /// <inheritdoc />
    public IOrganizationService SystemService => _systemService.Value;

    /// <inheritdoc />
    public void Trace(string message) => TracingService.Trace("{0}", message);

    private static T GetRequiredService<T>(IServiceProvider serviceProvider) where T : class
        => serviceProvider.GetService(typeof(T)) as T
           ?? throw new InvalidPluginExecutionException($"Service '{typeof(T).Name}' is not available.");
}