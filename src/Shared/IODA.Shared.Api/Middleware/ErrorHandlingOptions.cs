using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace IODA.Shared.Api.Middleware;

/// <summary>
/// Options for the shared error handling middleware.
/// Each API provides an exception mapper for its domain-specific exceptions.
/// ValidationException is always handled by the middleware (400 + errors).
/// </summary>
public class ErrorHandlingOptions
{
    /// <summary>
    /// Maps an exception to (StatusCode, ProblemDetails). Return null to use default 500 response.
    /// The middleware handles ValidationException internally; this mapper is only called for other exceptions.
    /// </summary>
    public required Func<Exception, IHostEnvironment?, (HttpStatusCode StatusCode, ProblemDetails Details)?> ExceptionMapper { get; init; }
}
