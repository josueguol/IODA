using IODA.Shared.BuildingBlocks.Domain;

namespace IODA.Authorization.Domain.Entities;

/// <summary>
/// Permiso que puede ser concedido en un rol (ej. content.read, content.write, content.publish).
/// </summary>
public class Permission : AggregateRoot<Guid>
{
    public string Code { get; private set; } = null!;
    public string Description { get; private set; } = null!;

    private Permission() { }

    private Permission(Guid id, string code, string description)
    {
        Id = id;
        Code = code;
        Description = description;
    }

    public static Permission Create(string code, string description)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Permission code cannot be empty", nameof(code));
        return new Permission(Guid.NewGuid(), code.Trim(), description?.Trim() ?? string.Empty);
    }

    public void UpdateDescription(string description)
    {
        Description = description?.Trim() ?? string.Empty;
    }
}
