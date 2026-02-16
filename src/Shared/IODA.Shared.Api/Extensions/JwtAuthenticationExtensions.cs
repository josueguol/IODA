using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace IODA.Shared.Api.Extensions;

/// <summary>
/// Extensiones para registrar autenticación JWT de forma consistente en todos los APIs.
/// </summary>
public static class JwtAuthenticationExtensions
{
    /// <summary>
    /// Clave de desarrollo usada cuando Jwt:SecretKey no está configurada. Debe coincidir con la usada en Identity/Authorization (p. ej. docker-compose).
    /// </summary>
    private const string DEVELOPMENT_SECRET_FALLBACK = "your-super-secret-key-min-32-chars-change-in-production";

    /// <summary>
    /// Registra autenticación JWT. Si Jwt:SecretKey está vacía, usa una clave de desarrollo para evitar "No authenticationScheme was specified"
    /// (los controladores con [Authorize] devolverían 401 en lugar de 400). En producción debe configurarse Jwt:SecretKey explícitamente.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSecret = configuration["Jwt:SecretKey"];
        if (string.IsNullOrEmpty(jwtSecret))
        {
            jwtSecret = DEVELOPMENT_SECRET_FALLBACK;
        }

        var jwtIssuer = configuration["Jwt:Issuer"];
        var jwtAudience = configuration["Jwt:Audience"] ?? "ioda-cms";

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        return services;
    }
}
