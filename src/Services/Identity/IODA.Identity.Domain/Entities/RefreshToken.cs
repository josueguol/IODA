using IODA.Shared.BuildingBlocks.Domain;

namespace IODA.Identity.Domain.Entities;

/// <summary>
/// Token de refresco para obtener un nuevo access token sin volver a enviar credenciales.
/// </summary>
public class RefreshToken : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsValid => !IsRevoked && !IsExpired;

    private RefreshToken() { }

    private RefreshToken(Guid id, Guid userId, string token, DateTime expiresAt, DateTime createdAt)
    {
        Id = id;
        UserId = userId;
        Token = token;
        ExpiresAt = expiresAt;
        CreatedAt = createdAt;
    }

    public static RefreshToken Create(Guid userId, string token, TimeSpan validity)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty", nameof(userId));
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token cannot be empty", nameof(token));

        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var expiresAt = now.Add(validity);
        return new RefreshToken(id, userId, token, expiresAt, now);
    }

    public void Revoke()
    {
        RevokedAt = DateTime.UtcNow;
    }
}
