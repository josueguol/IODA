using IODA.Publishing.Domain.Entities;

namespace IODA.Publishing.Domain.Repositories;

public interface IPublicationRequestRepository
{
    Task<PublicationRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PublicationRequest>> GetByContentIdAsync(Guid contentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PublicationRequest>> GetByStatusAsync(PublicationRequestStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PublicationRequest>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PublicationRequest> AddAsync(PublicationRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(PublicationRequest request, CancellationToken cancellationToken = default);
}
