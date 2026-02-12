using System.Diagnostics;

namespace IODA.Core.API.Middleware;

/// <summary>
/// Registra cada request (método, path) y la respuesta (status code, duración).
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? string.Empty;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var statusCode = context.Response.StatusCode;
            _logger.LogInformation(
                "{Method} {Path} responded {StatusCode} in {ElapsedMs} ms",
                method,
                path,
                statusCode,
                stopwatch.ElapsedMilliseconds);
        }
    }
}
