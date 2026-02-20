namespace IODA.Core.Domain.Repositories;

public interface IContentHierarchyRepository
{
    Task<IReadOnlyList<Guid>> GetHierarchyIdsByContentIdAsync(Guid contentId, CancellationToken cancellationToken = default);
    Task ReplaceHierarchiesForContentAsync(Guid contentId, IReadOnlyList<Guid> hierarchyIds, CancellationToken cancellationToken = default);
}
