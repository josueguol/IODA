using IODA.Publishing.Domain.Entities;
using IODA.Publishing.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace IODA.Publishing.Infrastructure.Persistence.Repositories;

public class PublicationRequestRepository : IPublicationRequestRepository
{
    private readonly PublishingDbContext _context;

    public PublicationRequestRepository(PublishingDbContext context)
    {
        _context = context;
    }

    public async Task<PublicationRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.PublicationRequests.FindAsync([id], cancellationToken);

    public async Task<IReadOnlyList<PublicationRequest>> GetByContentIdAsync(Guid contentId, CancellationToken cancellationToken = default) =>
        await _context.PublicationRequests
            .Where(p => p.ContentId == contentId)
            .OrderByDescending(p => p.RequestedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PublicationRequest>> GetByStatusAsync(PublicationRequestStatus status, CancellationToken cancellationToken = default) =>
        await _context.PublicationRequests
            .Where(p => p.Status == status)
            .OrderByDescending(p => p.RequestedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PublicationRequest>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.PublicationRequests
            .OrderByDescending(p => p.RequestedAt)
            .ToListAsync(cancellationToken);

    public async Task<PublicationRequest> AddAsync(PublicationRequest request, CancellationToken cancellationToken = default)
    {
        await _context.PublicationRequests.AddAsync(request, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return request;
    }

    public async Task UpdateAsync(PublicationRequest request, CancellationToken cancellationToken = default)
    {
        _context.PublicationRequests.Update(request);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
