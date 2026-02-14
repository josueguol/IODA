namespace IODA.Identity.Application.Interfaces;

/// <summary>
/// Genera tokens JWT (access token) para un usuario identificado.
/// Incluye claims de permiso (tipo "permission") para policies RequireClaim en otras APIs.
/// </summary>
public interface IJwtTokenGenerator
{
    /// <summary>
    /// Nombre del claim usado para cada código de permiso (coherente con policies por permiso).
    /// </summary>
    const string PermissionClaimType = "permission";

    string GenerateAccessToken(
        Guid userId,
        string email,
        IEnumerable<string>? roles = null,
        IEnumerable<string>? permissionCodes = null);
    int GetAccessTokenExpirationMinutes();
}
