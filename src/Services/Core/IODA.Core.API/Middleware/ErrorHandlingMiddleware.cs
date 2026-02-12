using System.Net;
using System.Text.Json;
using FluentValidation;
using IODA.Core.Domain.Exceptions;
using IODA.Shared.BuildingBlocks.Domain;
using Microsoft.AspNetCore.Mvc;

namespace IODA.Core.API.Middleware;

/// <summary>
/// Captura excepciones no controladas y devuelve respuestas HTTP consistentes (ProblemDetails).
/// </summary>
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
        (HttpStatusCode statusCode, ProblemDetails? problemDetails) = exception switch
        {
            ValidationException validationEx => CreateValidationErrorResponse(validationEx),
            SchemaValidationException schemaEx => CreateSchemaValidationErrorResponse(schemaEx),
            ContentNotFoundException notFoundEx => (
                HttpStatusCode.NotFound,
                CreateProblemDetails(
                    statusCode: (int)HttpStatusCode.NotFound,
                    title: "Not Found",
                    detail: notFoundEx.Message)),

            SiteNotFoundException siteNotFoundEx => (
                HttpStatusCode.NotFound,
                CreateProblemDetails(
                    statusCode: (int)HttpStatusCode.NotFound,
                    title: "Site Not Found",
                    detail: siteNotFoundEx.Message)),

            DomainException domainEx => (
                HttpStatusCode.BadRequest,
                CreateProblemDetails(
                    statusCode: (int)HttpStatusCode.BadRequest,
                    title: "Domain Error",
                    detail: domainEx.Message)),

            ArgumentException argEx => (
                HttpStatusCode.BadRequest,
                CreateProblemDetails(
                    statusCode: (int)HttpStatusCode.BadRequest,
                    title: "Bad Request",
                    detail: argEx.Message)),

            InvalidOperationException opEx => (
                HttpStatusCode.BadRequest,
                CreateProblemDetails(
                    statusCode: (int)HttpStatusCode.BadRequest,
                    title: "Invalid Operation",
                    detail: opEx.Message)),

            _ => (
                HttpStatusCode.InternalServerError,
                CreateProblemDetails(
                    statusCode: (int)HttpStatusCode.InternalServerError,
                    title: "Internal Server Error",
                    detail: context.RequestServices.GetService<IHostEnvironment>()?.IsDevelopment() == true
                        ? exception.Message
                        : "An unexpected error occurred."))
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
        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails!, JsonOptions));
    }

    private static (HttpStatusCode, ProblemDetails) CreateValidationErrorResponse(ValidationException validationEx)
    {
        var errors = validationEx.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        var problem = CreateProblemDetails(
            statusCode: (int)HttpStatusCode.BadRequest,
            title: "Validation Error",
            detail: "One or more validation errors occurred.",
            errors: errors);
        return (HttpStatusCode.BadRequest, problem);
    }

    private static (HttpStatusCode, ProblemDetails) CreateSchemaValidationErrorResponse(SchemaValidationException schemaEx)
    {
        var errors = schemaEx.Errors
            .GroupBy(e => e.Field)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Message).ToArray());
        var problem = CreateProblemDetails(
            statusCode: (int)HttpStatusCode.BadRequest,
            title: "Schema Validation Error",
            detail: "Content does not conform to the schema.",
            errors: errors);
        return (HttpStatusCode.BadRequest, problem);
    }

    private static ProblemDetails CreateProblemDetails(
        int statusCode,
        string title,
        string detail,
        Dictionary<string, string[]>? errors = null)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail
        };
        if (errors != null)
        {
            problem.Extensions ??= new Dictionary<string, object?>();
            problem.Extensions["errors"] = errors;
        }
        return problem;
    }
}
