using FCG_CATALOG_API.Domain.Interfaces;
using FCG_CATALOG_API.Infra.Consumers;
using FCG_CATALOG_API.Infra.Repositories;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class ConfigureInfra
{
    private static void AddRepository(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepositoryBase<>), typeof(RepositoryBase<>));
        services.AddScoped<IGameRepository, GameRepository>();
        services.AddScoped<IAcquisitionRepository, AcquisitionRepository>();
    }

    private static void AddRabbitMq(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            x.AddConsumer<PaymentProcessedConsumer>();

            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(configuration["RabbitMq:Host"] ?? "localhost", h =>
                {
                    h.Username(configuration["RabbitMq:Username"] ?? "guest");
                    h.Password(configuration["RabbitMq:Password"] ?? "guest");
                });

                cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
                cfg.ConfigureEndpoints(ctx);
            });
        });
    }

    public static void AddConfigureInfra(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRepository();
        services.AddRabbitMq(configuration);
    }
}
