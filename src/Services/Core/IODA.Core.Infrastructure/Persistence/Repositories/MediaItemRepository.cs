using IODA.Core.Domain.Entities;
using IODA.Core.Domain.Repositories;
using IODA.Core.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace IODA.Core.Infrastructure.Persistence.Repositories;

public class MediaItemRepository : IMediaItemRepository
{
    private readonly CoreDbContext _context;

    public MediaItemRepository(CoreDbContext context)
    {
        _context = context;
    }

    public async Task<MediaItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.MediaItems
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<MediaItem?> GetByPublicIdAsync(Guid projectId, string publicId, CancellationToken cancellationToken = default)
    {
        var id = Identifier.FromString(publicId);
        return await _context.MediaItems
            .FirstOrDefaultAsync(m => m.ProjectId == projectId && m.PublicId == id, cancellationToken);
    }

    public async Task<IReadOnlyList<MediaItem>> GetByProjectAsync(Guid projectId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.MediaItems
            .Where(m => m.ProjectId == projectId)
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _context.MediaItems
            .CountAsync(m => m.ProjectId == projectId, cancellationToken);
    }

    public async Task AddAsync(MediaItem mediaItem, CancellationToken cancellationToken = default)
    {
        await _context.MediaItems.AddAsync(mediaItem, cancellationToken);
    }

    public Task DeleteAsync(MediaItem mediaItem, CancellationToken cancellationToken = default)
    {
        _context.MediaItems.Remove(mediaItem);
        return Task.CompletedTask;
    }
}
