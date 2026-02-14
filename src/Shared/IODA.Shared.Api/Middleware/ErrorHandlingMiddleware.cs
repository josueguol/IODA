using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace IODA.Shared.Api.Middleware;

/// <summary>
/// Shared middleware that catches unhandled exceptions and returns consistent ProblemDetails responses.
/// Handles ValidationException (FluentValidation) internally; other exceptions are mapped via options.ExceptionMapper.
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly ErrorHandlingOptions _options;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public ErrorHandlingMiddleware(
        RequestDelegate next,
        ILogger<ErrorHandlingMiddleware> logger,
        ErrorHandlingOptions options)
    {
        _next = next;
        _logger = logger;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        (HttpStatusCode statusCode, ProblemDetails problemDetails) = exception switch
        {
            ValidationException validationEx => CreateValidationErrorResponse(validationEx),
            _ => MapWithOptions(context, exception)
        };

        if ((int)statusCode >= 500)
        {
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        }
        else
        {
            _logger.LogWarning(exception, "Request error: {Message}", exception.Message);
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, JsonOptions));
    }

    private (HttpStatusCode, ProblemDetails) MapWithOptions(HttpContext context, Exception exception)
    {
        var env = context.RequestServices.GetService<IHostEnvironment>();
        var mapped = _options.ExceptionMapper(exception, env);
        if (mapped.HasValue)
        {
            return mapped.Value;
        }

        return (
            HttpStatusCode.InternalServerError,
            new ProblemDetails
            {
                Status = (int)HttpStatusCode.InternalServerError,
                Title = "Internal Server Error",
                Detail = env?.IsDevelopment() == true ? exception.Message : "An unexpected error occurred."
            });
    }

    private static (HttpStatusCode, ProblemDetails) CreateValidationErrorResponse(ValidationException validationEx)
    {
        var errors = validationEx.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        var problem = new ProblemDetails
        {
            Status = (int)HttpStatusCode.BadRequest,
            Title = "Validation Error",
            Detail = "One or more validation errors occurred."
        };
        problem.Extensions ??= new Dictionary<string, object?>();
        problem.Extensions["errors"] = errors;
        return (HttpStatusCode.BadRequest, problem);
    }
}
