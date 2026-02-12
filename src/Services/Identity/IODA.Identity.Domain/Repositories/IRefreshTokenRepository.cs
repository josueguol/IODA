namespace IODA.Identity.Domain.Repositories;

/// <summary>
/// Repositorio de refresh tokens.
/// </summary>
public interface IRefreshTokenRepository
{
    Task<Entities.RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<Entities.RefreshToken> AddAsync(Entities.RefreshToken refreshToken, CancellationToken cancellationToken = default);
    Task UpdateAsync(Entities.RefreshToken refreshToken, CancellationToken cancellationToken = default);
    Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
