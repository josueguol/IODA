namespace IODA.Core.Domain.Repositories;

public interface IContentSiteRepository
{
    Task<IReadOnlyList<Guid>> GetSiteIdsByContentIdAsync(Guid contentId, CancellationToken cancellationToken = default);
    Task ReplaceSitesForContentAsync(Guid contentId, IReadOnlyList<Guid> siteIds, CancellationToken cancellationToken = default);
}
