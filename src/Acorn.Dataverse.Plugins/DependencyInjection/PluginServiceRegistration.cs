using Acorn.Dataverse.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xrm.Sdk;

namespace Acorn.Dataverse.Plugins.DependencyInjection;

internal static class PluginServiceRegistration
{
    public static IServiceCollection AddPluginContextServices(this IServiceCollection services)
    {
        services.AddScoped<PluginContextHolder>();
        services.AddScoped<ILocalPluginContext>(sp => sp.GetRequiredService<PluginContextHolder>().Context);
        services.AddScoped<IPluginExecutionContext>(sp => sp.GetRequiredService<ILocalPluginContext>().ExecutionContext);
        services.AddScoped<ITracingService>(sp => sp.GetRequiredService<ILocalPluginContext>().TracingService);
        services.AddScoped<IOrganizationService>(sp => sp.GetRequiredService<ILocalPluginContext>().OrganizationService);
        services.AddScoped<IOrganizationServiceProvider>(
            sp => new PluginOrganizationServiceProvider(sp.GetRequiredService<ILocalPluginContext>()));

        return services;
    }
}