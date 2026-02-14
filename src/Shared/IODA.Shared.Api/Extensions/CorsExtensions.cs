using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IODA.Shared.Api.Extensions;

/// <summary>
/// Extensiones para configurar CORS con orígenes desde configuración o valores por defecto en Development.
/// </summary>
public static class CorsExtensions
{
    /// <summary>
    /// Añade CORS con política por defecto: orígenes desde Cors:AllowedOrigins; en Development sin config usa localhost:3000 y localhost:5173.
    /// </summary>
    public static IServiceCollection AddDefaultCors(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        if (environment.IsDevelopment() && allowedOrigins.Length == 0)
            allowedOrigins = new[] { "http://localhost:3000", "http://localhost:5173", "https://localhost:3000", "https://localhost:5173" };

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        return services;
    }
}
