namespace IODA.Core.Domain.Repositories;

/// <summary>
/// Repository interface for Content aggregate
/// </summary>
public interface IContentRepository
{
    Task<Entities.Content?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Entities.Content?> GetByPublicIdAsync(string publicId, CancellationToken cancellationToken = default);
    Task<Entities.Content?> GetBySlugAsync(Guid projectId, Guid environmentId, string slug, CancellationToken cancellationToken = default);
    Task<IEnumerable<Entities.Content>> GetByProjectAsync(Guid projectId, Guid? siteId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Entities.Content>> GetByEnvironmentAsync(Guid environmentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Entities.Content>> GetByContentTypeAsync(string contentType, CancellationToken cancellationToken = default);
    Task<IEnumerable<Entities.Content>> GetPublishedAsync(Guid projectId, Guid environmentId, Guid? siteId = null, CancellationToken cancellationToken = default);
    Task<Entities.Content> AddAsync(Entities.Content content, CancellationToken cancellationToken = default);
    Task UpdateAsync(Entities.Content content, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> CountByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
}
