using System.Security.Claims;

namespace IODA.Core.API.Extensions;

/// <summary>
/// Obtiene el identificador del usuario autenticado desde los claims del JWT (ADR-011).
/// El token incluye el claim "sub" con el UserId.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal? principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
            return null;

        var value = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst("sub")?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
