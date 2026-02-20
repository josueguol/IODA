using IODA.Core.Domain.Entities;
using IODA.Core.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace IODA.Core.Infrastructure.Persistence.Repositories;

public class HierarchyRepository : IHierarchyRepository
{
    private readonly CoreDbContext _context;

    public HierarchyRepository(CoreDbContext context)
    {
        _context = context;
    }

    public async Task<Hierarchy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Hierarchies
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Hierarchy>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        if (idList.Count == 0)
            return Array.Empty<Hierarchy>();
        return await _context.Hierarchies
            .Where(h => idList.Contains(h.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Hierarchy>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _context.Hierarchies
            .Where(h => h.ProjectId == projectId)
            .OrderBy(h => h.ParentHierarchyId == null ? 0 : 1)
            .ThenBy(h => h.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Hierarchy?> GetByProjectAndSlugAsync(Guid projectId, string slug, CancellationToken cancellationToken = default)
    {
        return await _context.Hierarchies
            .FirstOrDefaultAsync(h => h.ProjectId == projectId && h.Slug == slug, cancellationToken);
    }

    public async Task<IReadOnlyList<Hierarchy>> GetChildrenAsync(Guid? parentHierarchyId, Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _context.Hierarchies
            .Where(h => h.ProjectId == projectId && h.ParentHierarchyId == parentHierarchyId)
            .OrderBy(h => h.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetAncestorIdsAsync(Guid hierarchyId, int maxDepth, CancellationToken cancellationToken = default)
    {
        var ancestors = new List<Guid>();
        var currentId = hierarchyId;
        for (var i = 0; i < maxDepth; i++)
        {
            var parentId = await _context.Hierarchies
                .AsNoTracking()
                .Where(h => h.Id == currentId)
                .Select(h => h.ParentHierarchyId)
                .FirstOrDefaultAsync(cancellationToken);
            if (parentId == null || parentId == Guid.Empty)
                break;
            ancestors.Add(parentId.Value);
            currentId = parentId.Value;
        }
        return ancestors;
    }

    public async Task<bool> ExistsWithSlugAsync(Guid projectId, string slug, Guid? excludeHierarchyId, CancellationToken cancellationToken = default)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        var query = _context.Hierarchies.Where(h => h.ProjectId == projectId && h.Slug == normalized);
        if (excludeHierarchyId.HasValue)
            query = query.Where(h => h.Id != excludeHierarchyId.Value);
        return await query.AnyAsync(cancellationToken);
    }

    public async Task<Hierarchy> AddAsync(Hierarchy hierarchy, CancellationToken cancellationToken = default)
    {
        await _context.Hierarchies.AddAsync(hierarchy, cancellationToken);
        return hierarchy;
    }

    public Task UpdateAsync(Hierarchy hierarchy, CancellationToken cancellationToken = default)
    {
        _context.Hierarchies.Update(hierarchy);
        return Task.CompletedTask;
    }

    public async Task<bool> HasChildrenAsync(Guid hierarchyId, CancellationToken cancellationToken = default)
    {
        return await _context.Hierarchies
            .AnyAsync(h => h.ParentHierarchyId == hierarchyId, cancellationToken);
    }

    public Task DeleteAsync(Hierarchy hierarchy, CancellationToken cancellationToken = default)
    {
        _context.Hierarchies.Remove(hierarchy);
        return Task.CompletedTask;
    }
}
