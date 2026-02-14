using IODA.Authorization.Application.Permissions;
using IODA.Authorization.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IODA.Authorization.Infrastructure.Persistence;

/// <summary>
/// Inserta los permisos del catálogo en BD al arranque si faltan. Idempotente por código (no duplica).
/// </summary>
public class PermissionSeeder
{
    private readonly AuthorizationDbContext _context;

    public PermissionSeeder(AuthorizationDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existingCodes = await _context.Permissions
            .Select(p => p.Code)
            .ToHashSetAsync(StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var definition in PermissionCatalog.All)
        {
            if (existingCodes.Contains(definition.Code))
                continue;

            var permission = Permission.Create(definition.Code, definition.Description);
            _context.Permissions.Add(permission);
            existingCodes.Add(definition.Code);
        }

        if (_context.ChangeTracker.HasChanges())
            await _context.SaveChangesAsync(cancellationToken);
    }
}
