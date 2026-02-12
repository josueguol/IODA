using IODA.Shared.BuildingBlocks.Domain;

namespace IODA.Authorization.Domain.Entities;

/// <summary>
/// Rol que agrupa permisos (ej. Editor, Publisher, Admin).
/// </summary>
public class Role : AggregateRoot<Guid>
{
    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = null!;

    private readonly List<RolePermission> _rolePermissions = [];
    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();

    private Role() { }

    private Role(Guid id, string name, string description)
    {
        Id = id;
        Name = name;
        Description = description;
    }

    public static Role Create(string name, string description = "")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Role name cannot be empty", nameof(name));
        return new Role(Guid.NewGuid(), name.Trim(), description?.Trim() ?? string.Empty);
    }

    public void UpdateDescription(string description)
    {
        Description = description?.Trim() ?? string.Empty;
    }

    public void GrantPermission(Guid permissionId)
    {
        if (_rolePermissions.Any(rp => rp.PermissionId == permissionId))
            return;
        _rolePermissions.Add(RolePermission.Create(Id, permissionId));
    }

    public void RevokePermission(Guid permissionId)
    {
        var toRemove = _rolePermissions.FirstOrDefault(rp => rp.PermissionId == permissionId);
        if (toRemove != null)
            _rolePermissions.Remove(toRemove);
    }

    public bool HasPermission(Guid permissionId) =>
        _rolePermissions.Any(rp => rp.PermissionId == permissionId);
}
