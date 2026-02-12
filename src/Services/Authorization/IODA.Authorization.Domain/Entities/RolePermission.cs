using IODA.Shared.BuildingBlocks.Domain;

namespace IODA.Authorization.Domain.Entities;

/// <summary>
/// Relación muchos-a-muchos entre Role y Permission.
/// </summary>
public class RolePermission : Entity<Guid>
{
    public Guid RoleId { get; private set; }
    public Guid PermissionId { get; private set; }

    private RolePermission() { }

    internal static RolePermission Create(Guid roleId, Guid permissionId)
    {
        return new RolePermission
        {
            Id = Guid.NewGuid(),
            RoleId = roleId,
            PermissionId = permissionId
        };
    }
}
