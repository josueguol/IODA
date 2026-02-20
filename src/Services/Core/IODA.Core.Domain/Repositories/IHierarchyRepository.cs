namespace IODA.Core.Domain.Repositories;

public interface IHierarchyRepository
{
    Task<Entities.Hierarchy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Entities.Hierarchy>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Entities.Hierarchy>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<Entities.Hierarchy?> GetByProjectAndSlugAsync(Guid projectId, string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Entities.Hierarchy>> GetChildrenAsync(Guid? parentHierarchyId, Guid projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetAncestorIdsAsync(Guid hierarchyId, int maxDepth, CancellationToken cancellationToken = default);
    Task<bool> ExistsWithSlugAsync(Guid projectId, string slug, Guid? excludeHierarchyId, CancellationToken cancellationToken = default);
    Task<Entities.Hierarchy> AddAsync(Entities.Hierarchy hierarchy, CancellationToken cancellationToken = default);
    Task UpdateAsync(Entities.Hierarchy hierarchy, CancellationToken cancellationToken = default);
    Task<bool> HasChildrenAsync(Guid hierarchyId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Entities.Hierarchy hierarchy, CancellationToken cancellationToken = default);
}
