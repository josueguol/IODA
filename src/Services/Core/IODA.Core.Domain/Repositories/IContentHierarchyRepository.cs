namespace IODA.Core.Domain.Repositories;

public interface IContentHierarchyRepository
{
    Task<IReadOnlyList<Guid>> GetHierarchyIdsByContentIdAsync(Guid contentId, CancellationToken cancellationToken = default);
    Task<Guid?> GetPrimaryHierarchyIdByContentIdAsync(Guid contentId, CancellationToken cancellationToken = default);
    Task ReplaceHierarchiesForContentAsync(
        Guid contentId,
        IReadOnlyList<Guid> hierarchyIds,
        Guid? primaryHierarchyId = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetContentIdsByHierarchyIdsAsync(IReadOnlyList<Guid> hierarchyIds, CancellationToken cancellationToken = default);
}
