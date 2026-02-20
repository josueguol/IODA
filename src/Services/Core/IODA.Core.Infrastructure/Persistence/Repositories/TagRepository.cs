using IODA.Core.Domain.Entities;
using IODA.Core.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace IODA.Core.Infrastructure.Persistence.Repositories;

public class TagRepository : ITagRepository
{
    private readonly CoreDbContext _context;

    public TagRepository(CoreDbContext context)
    {
        _context = context;
    }

    public async Task<Tag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Tags
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Tag>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        if (idList.Count == 0)
            return Array.Empty<Tag>();
        return await _context.Tags
            .Where(t => idList.Contains(t.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Tag>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _context.Tags
            .Where(t => t.ProjectId == projectId)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Tag?> GetByProjectAndSlugAsync(Guid projectId, string slug, CancellationToken cancellationToken = default)
    {
        return await _context.Tags
            .FirstOrDefaultAsync(t => t.ProjectId == projectId && t.Slug == slug, cancellationToken);
    }

    public async Task<Tag> AddAsync(Tag tag, CancellationToken cancellationToken = default)
    {
        await _context.Tags.AddAsync(tag, cancellationToken);
        return tag;
    }

    public async Task<bool> ExistsWithSlugAsync(Guid projectId, string slug, CancellationToken cancellationToken = default)
    {
        return await _context.Tags
            .AnyAsync(t => t.ProjectId == projectId && t.Slug == slug, cancellationToken);
    }
}
