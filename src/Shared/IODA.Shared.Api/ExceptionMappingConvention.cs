using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace IODA.Shared.Api;

/// <summary>
/// Convención unificada para mapear excepciones de sistema a respuestas HTTP en todos los APIs.
/// Usar en el ExceptionMapper de cada API después de manejar excepciones de dominio.
/// </summary>
/// <remarks>
/// Reglas: ArgumentException → 400 Bad Request.
/// InvalidOperationException con mensaje de conflicto (p. ej. "already exists") → 409 Conflict; en caso contrario → 400 Bad Request.
/// </remarks>
public static class ExceptionMappingConvention
{
    private const string ConflictMessagePattern = "already exists";

    /// <summary>
    /// Mapea ArgumentException e InvalidOperationException según la convención. Devuelve null para otras excepciones.
    /// </summary>
    public static (HttpStatusCode StatusCode, ProblemDetails Details)? Map(Exception ex)
    {
        if (ex is ArgumentException argEx)
        {
            return (HttpStatusCode.BadRequest, new ProblemDetails
            {
                Status = 400,
                Title = "Bad Request",
                Detail = argEx.Message
            });
        }

        if (ex is InvalidOperationException opEx)
        {
            var isConflict = opEx.Message.Contains(ConflictMessagePattern, StringComparison.OrdinalIgnoreCase);
            return (isConflict ? HttpStatusCode.Conflict : HttpStatusCode.BadRequest, new ProblemDetails
            {
                Status = isConflict ? 409 : 400,
                Title = isConflict ? "Conflict" : "Invalid Operation",
                Detail = opEx.Message
            });
        }

        return null;
    }
}
