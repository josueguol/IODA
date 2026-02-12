using System.Net;
using System.Text.Json;
using FluentValidation;
using IODA.Publishing.Application.Exceptions;
using IODA.Publishing.Domain.Exceptions;
using IODA.Shared.BuildingBlocks.Domain;
using Microsoft.AspNetCore.Mvc;

namespace IODA.Publishing.API.Middleware;

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
            CoreApiException coreEx => CreateCoreApiError(coreEx),
            PublicationRequestNotFoundException => ((HttpStatusCode)HttpStatusCode.NotFound, CreateProblem(404, "Not Found", exception.Message)),
            DomainException domainEx => ((HttpStatusCode)HttpStatusCode.BadRequest, CreateProblem(400, "Bad Request", domainEx.Message)),
            InvalidOperationException opEx => ((HttpStatusCode)HttpStatusCode.BadRequest, CreateProblem(400, "Bad Request", opEx.Message)),
            HttpRequestException httpEx => ((HttpStatusCode)HttpStatusCode.BadGateway, CreateProblem(502, "Bad Gateway", "Core API unavailable or returned an error.")),
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

    private static (HttpStatusCode, ProblemDetails) CreateCoreApiError(CoreApiException ex)
    {
        // Si Core API devolvió ProblemDetails, exponerlos; si no, crear uno genérico
        if (ex.ProblemDetails != null)
        {
            // Mantener los detalles de Core pero ajustar el título para indicar que viene de Core API
            var problem = new ProblemDetails
            {
                Status = ex.ProblemDetails.Status ?? ex.StatusCode,
                Title = $"Core API Error: {ex.ProblemDetails.Title ?? "Error"}",
                Detail = ex.ProblemDetails.Detail ?? ex.Message
            };
            
            // Copiar las extensiones (incluyendo "errors" si hay errores de validación)
            if (ex.ProblemDetails.Extensions != null)
            {
                problem.Extensions = new Dictionary<string, object?>(ex.ProblemDetails.Extensions);
            }
            
            // Añadir información adicional sobre el origen
            problem.Extensions ??= new Dictionary<string, object?>();
            problem.Extensions["coreApiStatusCode"] = ex.StatusCode;
            
            return ((HttpStatusCode)(problem.Status ?? ex.StatusCode), problem);
        }
        
        // Si no hay ProblemDetails, crear uno genérico con el mensaje
        return ((HttpStatusCode)ex.StatusCode, CreateProblem(ex.StatusCode, "Core API Error", ex.Message));
    }

    private static ProblemDetails CreateProblem(int statusCode, string title, string detail)
    {
        return new ProblemDetails { Status = statusCode, Title = title, Detail = detail };
    }
}
