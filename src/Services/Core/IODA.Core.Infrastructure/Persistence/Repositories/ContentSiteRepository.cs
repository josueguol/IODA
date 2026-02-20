using IODA.Core.Domain.Entities;
using IODA.Core.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace IODA.Core.Infrastructure.Persistence.Repositories;

public class ContentSiteRepository : IContentSiteRepository
{
    private readonly CoreDbContext _context;

    public ContentSiteRepository(CoreDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Guid>> GetSiteIdsByContentIdAsync(Guid contentId, CancellationToken cancellationToken = default)
    {
        return await _context.ContentSites
            .AsNoTracking()
            .Where(cs => cs.ContentId == contentId)
            .Select(cs => cs.SiteId)
            .ToListAsync(cancellationToken);
    }

    public async Task ReplaceSitesForContentAsync(Guid contentId, IReadOnlyList<Guid> siteIds, CancellationToken cancellationToken = default)
    {
        var existing = await _context.ContentSites
            .Where(cs => cs.ContentId == contentId)
            .ToListAsync(cancellationToken);
        _context.ContentSites.RemoveRange(existing);

        foreach (var siteId in siteIds ?? Array.Empty<Guid>())
        {
            _context.ContentSites.Add(ContentSite.Create(contentId, siteId));
        }
    }
}
