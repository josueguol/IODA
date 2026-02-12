using IODA.Core.Domain.Entities;
using IODA.Core.Domain.Repositories;
using IODA.Core.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace IODA.Core.Infrastructure.Persistence.Repositories;

public class ContentRepository : IContentRepository
{
    private readonly CoreDbContext _context;

    public ContentRepository(CoreDbContext context)
    {
        _context = context;
    }

    public async Task<Content?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Contents
            .Include(c => c.Versions)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Content?> GetByPublicIdAsync(string publicId, CancellationToken cancellationToken = default)
    {
        return await _context.Contents
            .Include(c => c.Versions)
            .FirstOrDefaultAsync(c => c.PublicId.FullId == publicId, cancellationToken);
    }

    public async Task<Content?> GetBySlugAsync(
        Guid projectId,
        Guid environmentId,
        string slug,
        CancellationToken cancellationToken = default)
    {
        return await _context.Contents
            .Include(c => c.Versions)
            .FirstOrDefaultAsync(c =>
                c.ProjectId == projectId &&
                c.EnvironmentId == environmentId &&
                c.Slug.Value == slug,
                cancellationToken);
    }

    public async Task<IEnumerable<Content>> GetByProjectAsync(
        Guid projectId,
        Guid? siteId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Contents
            .Include(c => c.Versions)
            .Where(c => c.ProjectId == projectId);

        if (siteId.HasValue)
        {
            query = query.Where(c => c.SiteId == siteId.Value);
        }

        return await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Content>> GetByEnvironmentAsync(
        Guid environmentId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Contents
            .Include(c => c.Versions)
            .Where(c => c.EnvironmentId == environmentId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Content>> GetByContentTypeAsync(
        string contentType,
        CancellationToken cancellationToken = default)
    {
        return await _context.Contents
            .Include(c => c.Versions)
            .Where(c => c.ContentType == contentType)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Content>> GetPublishedAsync(
        Guid projectId,
        Guid environmentId,
        Guid? siteId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Contents
            .Include(c => c.Versions)
            .Where(c =>
                c.ProjectId == projectId &&
                c.EnvironmentId == environmentId &&
                c.Status == ContentStatus.Published);

        if (siteId.HasValue)
        {
            query = query.Where(c => c.SiteId == siteId.Value);
        }

        return await query
            .OrderByDescending(c => c.PublishedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Content> AddAsync(Content content, CancellationToken cancellationToken = default)
    {
        await _context.Contents.AddAsync(content, cancellationToken);
        return content;
    }

    public Task UpdateAsync(Content content, CancellationToken cancellationToken = default)
    {
        _context.Contents.Update(content);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var content = await _context.Contents.FindAsync([id], cancellationToken);
        if (content != null)
        {
            _context.Contents.Remove(content);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Contents.AnyAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<int> CountByProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _context.Contents.CountAsync(c => c.ProjectId == projectId, cancellationToken);
    }
}
