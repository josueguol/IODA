namespace IODA.Core.Domain.Repositories;

public interface IContentSiteUrlRepository
{
    Task<IReadOnlyList<Entities.ContentSiteUrl>> GetByContentIdAsync(Guid contentId, CancellationToken cancellationToken = default);
    Task<Entities.ContentSiteUrl?> GetBySiteAndPathAsync(Guid siteId, string path, CancellationToken cancellationToken = default);
    Task ReplaceForContentAsync(Guid contentId, IReadOnlyList<Entities.ContentSiteUrl> siteUrls, CancellationToken cancellationToken = default);
}
