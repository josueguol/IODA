using IODA.Publishing.Application.Interfaces;
using IODA.Publishing.Domain.Repositories;
using IODA.Publishing.Infrastructure.CoreApi;
using IODA.Publishing.Infrastructure.Persistence;
using IODA.Publishing.Infrastructure.Persistence.Repositories;
using IODA.Publishing.Infrastructure.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IODA.Publishing.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<PublishingDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(PublishingDbContext).Assembly.FullName);
                npgsql.EnableRetryOnFailure(3);
                npgsql.CommandTimeout(30);
            });
        });

        services.AddScoped<IPublicationRequestRepository, PublicationRequestRepository>();

        services.AddHttpClient<CorePublishClient>();
        services.AddScoped<ICorePublishClient>(sp => sp.GetRequiredService<CorePublishClient>());
        services.AddScoped<IContentValidator, ContentValidator>();

        return services;
    }
}
