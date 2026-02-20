namespace IODA.Core.Domain.Repositories;

public interface IContentTagRepository
{
    Task<IReadOnlyList<Guid>> GetTagIdsByContentIdAsync(Guid contentId, CancellationToken cancellationToken = default);
    Task ReplaceTagsForContentAsync(Guid contentId, IReadOnlyList<Guid> tagIds, CancellationToken cancellationToken = default);
}
