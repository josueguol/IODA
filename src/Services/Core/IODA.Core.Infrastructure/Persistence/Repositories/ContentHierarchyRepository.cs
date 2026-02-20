using IODA.Core.Domain.Entities;
using IODA.Core.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace IODA.Core.Infrastructure.Persistence.Repositories;

public class ContentHierarchyRepository : IContentHierarchyRepository
{
    private readonly CoreDbContext _context;

    public ContentHierarchyRepository(CoreDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Guid>> GetHierarchyIdsByContentIdAsync(Guid contentId, CancellationToken cancellationToken = default)
    {
        return await _context.ContentHierarchies
            .AsNoTracking()
            .Where(ch => ch.ContentId == contentId)
            .Select(ch => ch.HierarchyId)
            .ToListAsync(cancellationToken);
    }

    public async Task ReplaceHierarchiesForContentAsync(Guid contentId, IReadOnlyList<Guid> hierarchyIds, CancellationToken cancellationToken = default)
    {
        var existing = await _context.ContentHierarchies
            .Where(ch => ch.ContentId == contentId)
            .ToListAsync(cancellationToken);
        _context.ContentHierarchies.RemoveRange(existing);

        foreach (var hierarchyId in hierarchyIds ?? Array.Empty<Guid>())
        {
            _context.ContentHierarchies.Add(ContentHierarchy.Create(contentId, hierarchyId));
        }
    }
}
