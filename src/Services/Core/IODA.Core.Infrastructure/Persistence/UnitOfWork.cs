using IODA.Core.Domain.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace IODA.Core.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly CoreDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(CoreDbContext context)
    {
        _context = context;
        Projects = new Repositories.ProjectRepository(context);
        Contents = new Repositories.ContentRepository(context);
        Schemas = new Repositories.ContentSchemaRepository(context);
        MediaItems = new Repositories.MediaItemRepository(context);
        Sites = new Repositories.SiteRepository(context);
        Tags = new Repositories.TagRepository(context);
        ContentTags = new Repositories.ContentTagRepository(context);
        Hierarchies = new Repositories.HierarchyRepository(context);
        ContentHierarchies = new Repositories.ContentHierarchyRepository(context);
        ContentSites = new Repositories.ContentSiteRepository(context);
    }

    public IProjectRepository Projects { get; }
    public IContentRepository Contents { get; }
    public IContentSchemaRepository Schemas { get; }
    public IMediaItemRepository MediaItems { get; }
    public ISiteRepository Sites { get; }
    public ITagRepository Tags { get; }
    public IContentTagRepository ContentTags { get; }
    public IHierarchyRepository Hierarchies { get; }
    public IContentHierarchyRepository ContentHierarchies { get; }
    public IContentSiteRepository ContentSites { get; }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
