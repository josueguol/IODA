using IODA.Authorization.Domain.Entities;

namespace IODA.Authorization.Domain.Repositories;

public interface IAccessRuleRepository
{
    Task<AccessRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccessRule>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<AccessRule> AddAsync(AccessRule rule, CancellationToken cancellationToken = default);
    Task UpdateAsync(AccessRule rule, CancellationToken cancellationToken = default);
    Task DeleteAsync(AccessRule rule, CancellationToken cancellationToken = default);
}
