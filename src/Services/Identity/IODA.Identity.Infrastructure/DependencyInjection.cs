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
        var authApiServiceKey = configuration[$"{AuthorizationApiOptions.SectionName}:ServiceApiKey"]?.Trim() ?? "";

        // En Development, si no está configurada la Authorization API, usar valores por defecto para que el primer usuario reciba SuperAdmin y el JWT incluya permisos.
        var env = configuration["ASPNETCORE_ENVIRONMENT"];
        if (string.IsNullOrEmpty(authApiBaseUrl) && string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase))
        {
            authApiBaseUrl = "http://localhost:5271";
            if (string.IsNullOrEmpty(authApiServiceKey))
                authApiServiceKey = "dev-service-api-key-32-chars-min";
        }

        if (!string.IsNullOrEmpty(authApiBaseUrl))
        {
            services.AddHttpClient(AuthorizationEffectivePermissionsClient.HttpClientName, (sp, client) =>
            {
                client.BaseAddress = new Uri(authApiBaseUrl + "/api/authorization/");
                if (!string.IsNullOrWhiteSpace(authApiServiceKey))
                    client.DefaultRequestHeaders.TryAddWithoutValidation("X-Service-Api-Key", authApiServiceKey);
            });
            services.AddScoped<IEffectivePermissionsClient, AuthorizationEffectivePermissionsClient>();
            services.AddScoped<IFirstUserBootstrapClient, AuthorizationBootstrapFirstUserClient>();
        }
        else
        {
            services.AddScoped<IEffectivePermissionsClient, NoOpEffectivePermissionsClient>();
            services.AddScoped<IFirstUserBootstrapClient, NoOpFirstUserBootstrapClient>();
        }

        return services;
    }
}

