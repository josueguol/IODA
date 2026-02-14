using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using IODA.Shared.Api.Middleware;
using Microsoft.AspNetCore.Mvc;

namespace IODA.Shared.Api;

/// <summary>
/// Extension methods for registering shared API components (error handling, JWT, CORS).
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the error handling options. Call before building the app; then use app.UseMiddleware&lt;ErrorHandlingMiddleware&gt;().
    /// </summary>
    public static IServiceCollection AddSharedErrorHandling(
        this IServiceCollection services,
        Func<Exception, IHostEnvironment?, (HttpStatusCode StatusCode, ProblemDetails Details)?> exceptionMapper)
    {
        services.AddSingleton(new ErrorHandlingOptions { ExceptionMapper = exceptionMapper });
        return services;
    }
}
