namespace IODA.Identity.Application.DTOs;

public record LoginResultDto(
    string AccessToken,
    string RefreshToken,
    int ExpiresInSeconds,
    Guid UserId,
    string Email,
    string? DisplayName);
