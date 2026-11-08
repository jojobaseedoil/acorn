using Microsoft.Extensions.DependencyInjection;

namespace Acorn.Samples.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSampleServices(this IServiceCollection services)
    {
        services.AddScoped<CreditLimitService>();
        return services;
    }
}