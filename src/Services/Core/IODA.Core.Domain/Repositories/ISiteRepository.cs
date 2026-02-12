using IODA.Core.Domain.Entities;

namespace IODA.Core.Domain.Repositories;

public interface ISiteRepository
{
    Task<Site?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Site?> GetByPublicIdAsync(Guid projectId, string publicId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Site>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Site>> GetByProjectAndEnvironmentAsync(Guid projectId, Guid environmentId, CancellationToken cancellationToken = default);
    Task<Site?> GetByDomainAsync(string domain, string? subdomain = null, string? subpath = null, CancellationToken cancellationToken = default);
    Task AddAsync(Site site, CancellationToken cancellationToken = default);
    Task UpdateAsync(Site site, CancellationToken cancellationToken = default);
    Task DeleteAsync(Site site, CancellationToken cancellationToken = default);
}
