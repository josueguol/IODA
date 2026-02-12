namespace IODA.Core.Domain.Repositories;

/// <summary>
/// Repository interface for Environment entity
/// </summary>
public interface IEnvironmentRepository
{
    Task<IEnumerable<Entities.Environment>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<Entities.Environment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Entities.Environment> AddAsync(Entities.Environment environment, CancellationToken cancellationToken = default);
}
