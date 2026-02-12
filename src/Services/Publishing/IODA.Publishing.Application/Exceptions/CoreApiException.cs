using Microsoft.AspNetCore.Mvc;

namespace IODA.Publishing.Application.Exceptions;

/// <summary>
/// Se lanza cuando el Core API devuelve un error (400, 404, etc.) con ProblemDetails.
/// Permite exponer los detalles del error de Core en los logs y respuestas de Publishing.
/// </summary>
public class CoreApiException : Exception
{
    public int StatusCode { get; }
    public ProblemDetails? ProblemDetails { get; }

    public CoreApiException(int statusCode, string message, ProblemDetails? problemDetails = null)
        : base(message)
    {
        StatusCode = statusCode;
        ProblemDetails = problemDetails;
    }
}
