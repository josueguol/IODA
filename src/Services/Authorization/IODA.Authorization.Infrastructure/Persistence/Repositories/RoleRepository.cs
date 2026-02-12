using IODA.Authorization.Domain.Entities;
using IODA.Authorization.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace IODA.Authorization.Infrastructure.Persistence.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly AuthorizationDbContext _context;

    public RoleRepository(AuthorizationDbContext context)
    {
        _context = context;
    }

    public async Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Roles.FindAsync([id], cancellationToken);

    public async Task<Role?> GetByIdWithPermissionsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.Roles.ToListAsync(cancellationToken);

    public async Task<Role> AddAsync(Role role, CancellationToken cancellationToken = default)
    {
        await _context.Roles.AddAsync(role, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return role;
    }

    public async Task UpdateAsync(Role role, CancellationToken cancellationToken = default)
    {
        _context.Roles.Update(role);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, Guid? excludeRoleId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Roles.Where(r => r.Name == name);
        if (excludeRoleId.HasValue)
            query = query.Where(r => r.Id != excludeRoleId.Value);
        return await query.AnyAsync(cancellationToken);
    }
}
