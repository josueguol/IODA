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

    public async Task<Guid?> GetPrimaryHierarchyIdByContentIdAsync(Guid contentId, CancellationToken cancellationToken = default)
    {
        return await _context.ContentHierarchies
            .AsNoTracking()
            .Where(ch => ch.ContentId == contentId && ch.IsPrimary)
            .Select(ch => (Guid?)ch.HierarchyId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task ReplaceHierarchiesForContentAsync(
        Guid contentId,
        IReadOnlyList<Guid> hierarchyIds,
        Guid? primaryHierarchyId = null,
        CancellationToken cancellationToken = default)
    {
        var existing = await _context.ContentHierarchies
            .Where(ch => ch.ContentId == contentId)
            .ToListAsync(cancellationToken);
        _context.ContentHierarchies.RemoveRange(existing);

        foreach (var hierarchyId in hierarchyIds ?? Array.Empty<Guid>())
        {
            var isPrimary = primaryHierarchyId.HasValue && primaryHierarchyId.Value == hierarchyId;
            _context.ContentHierarchies.Add(ContentHierarchy.Create(contentId, hierarchyId, isPrimary));
        }
    }

    public async Task<IReadOnlyList<Guid>> GetContentIdsByHierarchyIdsAsync(IReadOnlyList<Guid> hierarchyIds, CancellationToken cancellationToken = default)
    {
        if (hierarchyIds == null || hierarchyIds.Count == 0)
            return Array.Empty<Guid>();

        return await _context.ContentHierarchies
            .AsNoTracking()
            .Where(ch => hierarchyIds.Contains(ch.HierarchyId))
            .Select(ch => ch.ContentId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
