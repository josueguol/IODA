namespace IODA.Core.Domain.Repositories;

/// <summary>
/// Unit of Work pattern for coordinating multiple repository operations
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IProjectRepository Projects { get; }
    IContentRepository Contents { get; }
    IContentSchemaRepository Schemas { get; }
    IMediaItemRepository MediaItems { get; }
    ISiteRepository Sites { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
