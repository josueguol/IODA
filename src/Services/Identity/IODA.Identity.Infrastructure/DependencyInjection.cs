using IODA.Identity.Application.Interfaces;
using IODA.Identity.Domain.Repositories;
using IODA.Identity.Infrastructure.Clients;
using IODA.Identity.Infrastructure.Messaging;
using IODA.Identity.Infrastructure.Options;
using IODA.Identity.Infrastructure.Persistence;
using IODA.Identity.Infrastructure.Persistence.Repositories;
using IODA.Identity.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IODA.Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<IdentityDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(IdentityDbContext).Assembly.FullName);
                npgsql.EnableRetryOnFailure(3);
                npgsql.CommandTimeout(30);
            });
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IRefreshTokenGenerator, RefreshTokenGenerator>();
        services.AddScoped<IAuthEventPublisher, NoOpAuthEventPublisher>();
        services.AddSingleton<ISetupConfiguration, SetupConfiguration>();

        services.Configure<AuthorizationApiOptions>(configuration.GetSection(AuthorizationApiOptions.SectionName));
        var authApiBaseUrl = configuration[$"{AuthorizationApiOptions.SectionName}:BaseUrl"]?.TrimEnd('/') ?? "";
        if (!string.IsNullOrEmpty(authApiBaseUrl))
        {
            services.AddHttpClient(AuthorizationEffectivePermissionsClient.HttpClientName, (sp, client) =>
            {
                client.BaseAddress = new Uri(authApiBaseUrl + "/api/authorization/");
                var apiKey = configuration[$"{AuthorizationApiOptions.SectionName}:ServiceApiKey"];
                if (!string.IsNullOrWhiteSpace(apiKey))
                    client.DefaultRequestHeaders.TryAddWithoutValidation("X-Service-Api-Key", apiKey);
            });
            services.AddScoped<IEffectivePermissionsClient, AuthorizationEffectivePermissionsClient>();
        }
        else
        {
            services.AddScoped<IEffectivePermissionsClient, NoOpEffectivePermissionsClient>();
        }

        return services;
    }
}

