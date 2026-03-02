using IODA.Core.Domain.Entities;
using IODA.Core.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace IODA.Core.Infrastructure.Persistence.Repositories;

public class ContentSiteUrlRepository : IContentSiteUrlRepository
{
    private readonly CoreDbContext _context;

    public ContentSiteUrlRepository(CoreDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ContentSiteUrl>> GetByContentIdAsync(Guid contentId, CancellationToken cancellationToken = default)
    {
        return await _context.ContentSiteUrls
            .AsNoTracking()
            .Where(x => x.ContentId == contentId)
            .OrderBy(x => x.SiteId)
            .ToListAsync(cancellationToken);
    }

    public async Task<ContentSiteUrl?> GetBySiteAndPathAsync(Guid siteId, string path, CancellationToken cancellationToken = default)
    {
        var normalized = ContentSiteUrl.NormalizePath(path);
        return await _context.ContentSiteUrls
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.SiteId == siteId && x.Path == normalized, cancellationToken);
    }

    public async Task ReplaceForContentAsync(Guid contentId, IReadOnlyList<ContentSiteUrl> siteUrls, CancellationToken cancellationToken = default)
    {
        var existing = await _context.ContentSiteUrls
            .Where(x => x.ContentId == contentId)
            .ToListAsync(cancellationToken);
        _context.ContentSiteUrls.RemoveRange(existing);

        foreach (var siteUrl in siteUrls ?? Array.Empty<ContentSiteUrl>())
        {
            _context.ContentSiteUrls.Add(siteUrl);
        }
    }
}
