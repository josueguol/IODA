namespace IODA.Core.Domain.Repositories;

/// <summary>
/// Repository interface for Project aggregate
/// </summary>
public interface IProjectRepository
{
    Task<Entities.Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Entities.Project?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IEnumerable<Entities.Project>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<(IEnumerable<Entities.Project> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Entities.Project> AddAsync(Entities.Project project, CancellationToken cancellationToken = default);
    Task UpdateAsync(Entities.Project project, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
