using IODA.Core.Application.Interfaces;
using IODA.Core.Domain.Repositories;
using IODA.Core.Infrastructure.Messaging;
using IODA.Core.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IODA.Core.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<CoreDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(CoreDbContext).Assembly.FullName);
                npgsql.EnableRetryOnFailure(3);
                npgsql.CommandTimeout(30);
            });
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IProjectRepository>(sp => sp.GetRequiredService<IUnitOfWork>().Projects);
        services.AddScoped<IContentRepository>(sp => sp.GetRequiredService<IUnitOfWork>().Contents);
        services.AddScoped<IContentSchemaRepository>(sp => sp.GetRequiredService<IUnitOfWork>().Schemas);
        services.AddScoped<IMediaItemRepository>(sp => sp.GetRequiredService<IUnitOfWork>().MediaItems);
        services.AddScoped<ISiteRepository>(sp => sp.GetRequiredService<IUnitOfWork>().Sites);
        services.AddScoped<IEnvironmentRepository, Persistence.Repositories.EnvironmentRepository>();
        services.AddScoped<Application.Interfaces.IMediaStorage, Storage.LocalMediaStorage>();

        var rabbitEnabledRaw = configuration["RabbitMQ:Enabled"];
        var rabbitEnabled = string.IsNullOrEmpty(rabbitEnabledRaw) || !string.Equals(rabbitEnabledRaw, "false", StringComparison.OrdinalIgnoreCase);
        var rabbitHost = configuration["RabbitMQ:Host"];
        var useRabbitMq = rabbitEnabled && !string.IsNullOrWhiteSpace(rabbitHost);

        if (useRabbitMq)
        {
            services.AddScoped<IEventPublisher, MassTransitEventPublisher>();
            AddMassTransit(services, configuration);
        }
        else
        {
            services.AddScoped<IEventPublisher, NoOpEventPublisher>();
        }

        return services;
    }

    private static void AddMassTransit(IServiceCollection services, IConfiguration configuration)
    {
        var rabbitHost = configuration["RabbitMQ:Host"] ?? "localhost";
        var virtualHost = configuration["RabbitMQ:VirtualHost"] ?? "/";
        var username = configuration["RabbitMQ:Username"] ?? "guest";
        var password = configuration["RabbitMQ:Password"] ?? "guest";

        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitHost, virtualHost, h =>
                {
                    h.Username(username);
                    h.Password(password);
                });

                cfg.ConfigureEndpoints(context);
            });
        });
    }
}
