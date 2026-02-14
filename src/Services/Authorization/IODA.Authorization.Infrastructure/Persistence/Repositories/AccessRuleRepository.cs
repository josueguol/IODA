using IODA.Authorization.Domain.Entities;
using IODA.Authorization.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace IODA.Authorization.Infrastructure.Persistence.Repositories;

public class AccessRuleRepository : IAccessRuleRepository
{
    private readonly AuthorizationDbContext _context;

    public AccessRuleRepository(AuthorizationDbContext context)
    {
        _context = context;
    }

    public async Task<AccessRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.AccessRules.FindAsync([id], cancellationToken);

    public async Task<IReadOnlyList<AccessRule>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await _context.AccessRules
            .Where(a => a.UserId == userId)
            .ToListAsync(cancellationToken);

    public async Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        await _context.AccessRules.CountAsync(cancellationToken);

    public async Task<AccessRule> AddAsync(AccessRule rule, CancellationToken cancellationToken = default)
    {
        await _context.AccessRules.AddAsync(rule, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return rule;
    }

    public async Task UpdateAsync(AccessRule rule, CancellationToken cancellationToken = default)
    {
        _context.AccessRules.Update(rule);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(AccessRule rule, CancellationToken cancellationToken = default)
    {
        _context.AccessRules.Remove(rule);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
