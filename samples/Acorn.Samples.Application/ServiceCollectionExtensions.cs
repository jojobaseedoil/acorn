using Microsoft.Extensions.DependencyInjection;

namespace Acorn.Samples.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSampleServices(this IServiceCollection services)
    {
        services.AddScoped<CreditLimitService>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<INoteRepository, NoteRepository>();
        services.AddScoped<CreditLimitService>();
        
        return services;
    }
}