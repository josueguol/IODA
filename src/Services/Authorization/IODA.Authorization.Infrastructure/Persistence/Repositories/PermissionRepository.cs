using IODA.Authorization.Domain.Entities;
using IODA.Authorization.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace IODA.Authorization.Infrastructure.Persistence.Repositories;

public class PermissionRepository : IPermissionRepository
{
    private readonly AuthorizationDbContext _context;

    public PermissionRepository(AuthorizationDbContext context)
    {
        _context = context;
    }

    public async Task<Permission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Permissions.FindAsync([id], cancellationToken);

    public async Task<IReadOnlyList<Permission>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0)
            return Array.Empty<Permission>();
        return await _context.Permissions
            .Where(p => idList.Contains(p.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<Permission?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        await _context.Permissions.FirstOrDefaultAsync(p => p.Code == code, cancellationToken);

    public async Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.Permissions.ToListAsync(cancellationToken);

    public async Task<Permission> AddAsync(Permission permission, CancellationToken cancellationToken = default)
    {
        await _context.Permissions.AddAsync(permission, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return permission;
    }

    public async Task UpdateAsync(Permission permission, CancellationToken cancellationToken = default)
    {
        _context.Permissions.Update(permission);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        await _context.Permissions.AnyAsync(p => p.Code == code, cancellationToken);
}
