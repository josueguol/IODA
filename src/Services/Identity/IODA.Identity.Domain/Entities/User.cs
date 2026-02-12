using IODA.Shared.BuildingBlocks.Domain;

namespace IODA.Identity.Domain.Entities;

/// <summary>
/// Usuario del sistema. Responsable de autenticación (saber quién es).
/// </summary>
public class User : AggregateRoot<Guid>
{
    public string Email { get; private set; } = null!;
    public string NormalizedEmail { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string? DisplayName { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    private User() { }

    private User(
        Guid id,
        string email,
        string normalizedEmail,
        string passwordHash,
        string? displayName,
        bool isActive,
        DateTime createdAt)
    {
        Id = id;
        Email = email;
        NormalizedEmail = normalizedEmail;
        PasswordHash = passwordHash;
        DisplayName = displayName;
        IsActive = isActive;
        CreatedAt = createdAt;
    }

    public static User Create(
        string email,
        string passwordHash,
        string? displayName = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash cannot be empty", nameof(passwordHash));

        var id = Guid.NewGuid();
        var normalizedEmail = email.Trim().ToUpperInvariant();
        return new User(id, email.Trim(), normalizedEmail, passwordHash, displayName?.Trim(), true, DateTime.UtcNow);
    }

    public void UpdateDisplayName(string? displayName)
    {
        DisplayName = displayName?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash cannot be empty", nameof(passwordHash));
        PasswordHash = passwordHash;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
