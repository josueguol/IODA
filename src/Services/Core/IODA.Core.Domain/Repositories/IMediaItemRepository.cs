using IODA.Core.Domain.Entities;

namespace IODA.Core.Domain.Repositories;

public interface IMediaItemRepository
{
    Task<MediaItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MediaItem?> GetByPublicIdAsync(Guid projectId, string publicId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MediaItem>> GetByProjectAsync(Guid projectId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> CountByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task AddAsync(MediaItem mediaItem, CancellationToken cancellationToken = default);
    Task DeleteAsync(MediaItem mediaItem, CancellationToken cancellationToken = default);
}
