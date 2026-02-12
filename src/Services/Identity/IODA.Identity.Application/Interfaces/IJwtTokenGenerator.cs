namespace IODA.Identity.Application.Interfaces;

/// <summary>
/// Genera tokens JWT (access token) para un usuario identificado.
/// </summary>
public interface IJwtTokenGenerator
{
    string GenerateAccessToken(Guid userId, string email, IEnumerable<string>? roles = null);
    int GetAccessTokenExpirationMinutes();
}
