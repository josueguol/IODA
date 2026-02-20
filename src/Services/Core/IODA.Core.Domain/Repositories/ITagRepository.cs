namespace IODA.Core.Domain.Repositories;

public interface ITagRepository
{
    Task<Entities.Tag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Entities.Tag>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Entities.Tag>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<Entities.Tag?> GetByProjectAndSlugAsync(Guid projectId, string slug, CancellationToken cancellationToken = default);
    Task<Entities.Tag> AddAsync(Entities.Tag tag, CancellationToken cancellationToken = default);
    Task<bool> ExistsWithSlugAsync(Guid projectId, string slug, CancellationToken cancellationToken = default);
}
