using IODA.Core.Domain.Entities;
using IODA.Core.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace IODA.Core.Infrastructure.Persistence.Repositories;

public class ContentTagRepository : IContentTagRepository
{
    private readonly CoreDbContext _context;

    public ContentTagRepository(CoreDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Guid>> GetTagIdsByContentIdAsync(Guid contentId, CancellationToken cancellationToken = default)
    {
        return await _context.ContentTags
            .AsNoTracking()
            .Where(ct => ct.ContentId == contentId)
            .Select(ct => ct.TagId)
            .ToListAsync(cancellationToken);
    }

    public async Task ReplaceTagsForContentAsync(Guid contentId, IReadOnlyList<Guid> tagIds, CancellationToken cancellationToken = default)
    {
        var existing = await _context.ContentTags
            .Where(ct => ct.ContentId == contentId)
            .ToListAsync(cancellationToken);
        _context.ContentTags.RemoveRange(existing);

        foreach (var tagId in tagIds ?? Array.Empty<Guid>())
        {
            _context.ContentTags.Add(ContentTag.Create(contentId, tagId));
        }
    }
}
