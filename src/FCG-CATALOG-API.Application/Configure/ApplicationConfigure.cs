using FCG_CATALOG_API.Application.Interfaces;
using FCG_CATALOG_API.Application.Services;
using FCG_CATALOG_API.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

public static class ApplicationConfigure
{
    private static void ConfigureDependences(this IServiceCollection services)
    {
        services.AddScoped<IGameService, GameService>();
        services.AddScoped<IAcquisitionService, AcquisitionService>();
    }

    public static void AddApplicationConfiguration(this IServiceCollection serviceCollection)
    {
        serviceCollection.ConfigureDependences();
    }
}