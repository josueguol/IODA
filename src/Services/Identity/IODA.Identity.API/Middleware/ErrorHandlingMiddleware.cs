using System.Net;
using System.Text.Json;
using FluentValidation;
using IODA.Identity.Domain.Exceptions;
using IODA.Shared.BuildingBlocks.Domain;
using Microsoft.AspNetCore.Mvc;

namespace IODA.Identity.API.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
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
        var (statusCode, problemDetails) = exception switch
        {
            ValidationException validationEx => CreateValidationError(validationEx),
            InvalidCredentialsException => ((HttpStatusCode)HttpStatusCode.Unauthorized, CreateProblem(401, "Unauthorized", exception.Message)),
            InvalidRefreshTokenException => ((HttpStatusCode)HttpStatusCode.Unauthorized, CreateProblem(401, "Unauthorized", exception.Message)),
            UserNotFoundException => ((HttpStatusCode)HttpStatusCode.NotFound, CreateProblem(404, "Not Found", exception.Message)),
            UserAlreadyExistsException => ((HttpStatusCode)HttpStatusCode.Conflict, CreateProblem(409, "Conflict", exception.Message)),
            SelfRegistrationDisabledException => ((HttpStatusCode)HttpStatusCode.Forbidden, CreateProblem(403, "Forbidden", exception.Message)),
            DomainException domainEx => ((HttpStatusCode)HttpStatusCode.BadRequest, CreateProblem(400, "Bad Request", domainEx.Message)),
            _ => ((HttpStatusCode)HttpStatusCode.InternalServerError, CreateProblem(500, "Internal Server Error",
                context.RequestServices.GetService<IHostEnvironment>()?.IsDevelopment() == true ? exception.Message : "An unexpected error occurred."))
        };

        if ((int)statusCode >= 500)
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        else
            _logger.LogWarning(exception, "Request error: {Message}", exception.Message);

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, JsonOptions));
    }

    private static (HttpStatusCode, ProblemDetails) CreateValidationError(ValidationException ex)
    {
        var errors = ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        var problem = CreateProblem(400, "Validation Error", "One or more validation errors occurred.");
        problem.Extensions ??= new Dictionary<string, object?>();
        problem.Extensions["errors"] = errors;
        return (HttpStatusCode.BadRequest, problem);
    }

    private static ProblemDetails CreateProblem(int statusCode, string title, string detail)
    {
        return new ProblemDetails { Status = statusCode, Title = title, Detail = detail };
    }
}
