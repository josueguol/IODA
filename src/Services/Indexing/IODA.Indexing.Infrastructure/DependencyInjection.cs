using Elastic.Clients.Elasticsearch;
using IODA.Indexing.Application.Interfaces;
using IODA.Indexing.Infrastructure.Elasticsearch;
using IODA.Indexing.Infrastructure.Messaging;
using IODA.Indexing.Infrastructure.NoOp;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IODA.Indexing.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var esEnabledRaw = configuration["Elasticsearch:Enabled"];
        var esEnabled = !string.IsNullOrEmpty(esEnabledRaw) && string.Equals(esEnabledRaw, "true", StringComparison.OrdinalIgnoreCase);
        var esUrl = configuration["Elasticsearch:Url"];

        if (esEnabled && !string.IsNullOrWhiteSpace(esUrl))
        {
            services.AddSingleton<ElasticsearchClient>(sp =>
            {
                var cfg = sp.GetRequiredService<IConfiguration>();
                var url = cfg["Elasticsearch:Url"] ?? "http://localhost:9200";
                return new ElasticsearchClient(new ElasticsearchClientSettings(new Uri(url)));
            });
            services.AddScoped<IContentIndexer, ElasticsearchContentIndexer>();
        }
        else
        {
            services.AddScoped<IContentIndexer, NoOpContentIndexer>();
        }

        var rabbitEnabledRaw = configuration["RabbitMQ:Enabled"];
        var rabbitEnabled = !string.IsNullOrEmpty(rabbitEnabledRaw) && string.Equals(rabbitEnabledRaw, "true", StringComparison.OrdinalIgnoreCase);
        var rabbitHost = configuration["RabbitMQ:Host"];

        if (rabbitEnabled && !string.IsNullOrWhiteSpace(rabbitHost))
        {
            AddMassTransit(services, configuration);
        }

        return services;
    }

    private static void AddMassTransit(IServiceCollection services, IConfiguration configuration)
    {
        var rabbitHost = configuration["RabbitMQ:Host"] ?? "localhost";
        var virtualHost = configuration["RabbitMQ:VirtualHost"] ?? "ioda_cms";
        var username = configuration["RabbitMQ:Username"] ?? "guest";
        var password = configuration["RabbitMQ:Password"] ?? "guest";

        services.AddMassTransit(x =>
        {
            x.AddConsumer<ContentPublishedEventV1Consumer>();
            x.AddConsumer<ContentUnpublishedEventV1Consumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitHost, virtualHost, h =>
                {
                    h.Username(username);
                    h.Password(password);
                });

                cfg.ReceiveEndpoint("ioda-indexing-content-published", e =>
                {
                    e.ConfigureConsumer<ContentPublishedEventV1Consumer>(context);
                });
                cfg.ReceiveEndpoint("ioda-indexing-content-unpublished", e =>
                {
                    e.ConfigureConsumer<ContentUnpublishedEventV1Consumer>(context);
                });

                cfg.ConfigureEndpoints(context);
            });
        });
    }
}
