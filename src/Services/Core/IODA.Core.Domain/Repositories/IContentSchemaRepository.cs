namespace IODA.Core.Domain.Repositories;

/// <summary>
/// Repository interface for ContentSchema aggregate
/// </summary>
public interface IContentSchemaRepository
{
    Task<Entities.ContentSchema?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Entities.ContentSchema?> GetByTypeAsync(Guid projectId, string schemaType, CancellationToken cancellationToken = default);
    Task<IEnumerable<Entities.ContentSchema>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Entities.ContentSchema>> GetActiveByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<Entities.ContentSchema> AddAsync(Entities.ContentSchema schema, CancellationToken cancellationToken = default);
    Task UpdateAsync(Entities.ContentSchema schema, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> TypeExistsAsync(Guid projectId, string schemaType, CancellationToken cancellationToken = default);
}
