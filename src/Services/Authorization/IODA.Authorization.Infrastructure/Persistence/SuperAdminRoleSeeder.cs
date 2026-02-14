using IODA.Authorization.Application;
using IODA.Authorization.Application.Permissions;
using IODA.Authorization.Domain.Entities;
using IODA.Authorization.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace IODA.Authorization.Infrastructure.Persistence;

/// <summary>
/// 2.5: Crea el rol "SuperAdmin" si no existe y le asigna todos los permisos del catálogo.
/// Debe ejecutarse después de PermissionSeeder.
/// </summary>
public class SuperAdminRoleSeeder
{

    private readonly AuthorizationDbContext _context;
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;

    public SuperAdminRoleSeeder(
        AuthorizationDbContext context,
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository)
    {
        _context = context;
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var role = await _roleRepository.GetByNameAsync(AuthorizationConstants.SuperAdminRoleName, cancellationToken);
        if (role is null)
        {
            role = Role.Create(AuthorizationConstants.SuperAdminRoleName, "Super administrator with all catalog permissions");
            await _roleRepository.AddAsync(role, cancellationToken);
        }

        role = await _roleRepository.GetByIdWithPermissionsAsync(role.Id, cancellationToken);
        if (role is null)
            return;

        var catalogCodes = PermissionCatalog.AllCodes;
        var allPermissions = await _context.Permissions
            .Where(p => catalogCodes.Contains(p.Code))
            .ToListAsync(cancellationToken);

        foreach (var permission in allPermissions)
        {
            if (!role.HasPermission(permission.Id))
                role.GrantPermission(permission.Id);
        }

        await _roleRepository.UpdateAsync(role, cancellationToken);
    }
}
