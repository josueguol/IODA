using IODA.Core.Domain.Entities;
using IODA.Core.Domain.Repositories;
using IODA.Core.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace IODA.Core.Infrastructure.Persistence.Repositories;

public class SiteRepository : ISiteRepository
{
    private readonly CoreDbContext _context;

    public SiteRepository(CoreDbContext context)
    {
        _context = context;
    }

    public async Task<Site?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Sites
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<Site?> GetByPublicIdAsync(Guid projectId, string publicId, CancellationToken cancellationToken = default)
    {
        var id = Identifier.FromString(publicId);
        return await _context.Sites
            .FirstOrDefaultAsync(s => s.ProjectId == projectId && s.PublicId == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Site>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _context.Sites
            .Where(s => s.ProjectId == projectId)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Site>> GetByProjectAndEnvironmentAsync(Guid projectId, Guid environmentId, CancellationToken cancellationToken = default)
    {
        return await _context.Sites
            .Where(s => s.ProjectId == projectId && s.EnvironmentId == environmentId)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Site?> GetByDomainAsync(string domain, string? subdomain = null, string? subpath = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Sites.Where(s => s.Domain == domain && s.IsActive);
        
        if (subdomain != null)
            query = query.Where(s => s.Subdomain == subdomain);
        else
            query = query.Where(s => s.Subdomain == null || s.Subdomain == string.Empty);
        
        if (subpath != null)
            query = query.Where(s => s.Subpath == subpath);
        else
            query = query.Where(s => s.Subpath == null || s.Subpath == string.Empty);

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(Site site, CancellationToken cancellationToken = default)
    {
        await _context.Sites.AddAsync(site, cancellationToken);
    }

    public Task UpdateAsync(Site site, CancellationToken cancellationToken = default)
    {
        _context.Sites.Update(site);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Site site, CancellationToken cancellationToken = default)
    {
        _context.Sites.Remove(site);
        return Task.CompletedTask;
    }
}
