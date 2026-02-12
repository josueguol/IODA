using IODA.Shared.BuildingBlocks.Domain;

namespace IODA.Publishing.Domain.Entities;

/// <summary>
/// Solicitud de publicación de contenido. Flujo: Pending → Approved o Rejected.
/// </summary>
public class PublicationRequest : AggregateRoot<Guid>
{
    public Guid ContentId { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid EnvironmentId { get; private set; }
    public Guid RequestedBy { get; private set; }
    public PublicationRequestStatus Status { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public Guid? ResolvedBy { get; private set; }
    public string? RejectionReason { get; private set; }
    public string? ValidationErrors { get; private set; }

    private PublicationRequest() { }

    private PublicationRequest(
        Guid id,
        Guid contentId,
        Guid projectId,
        Guid environmentId,
        Guid requestedBy)
    {
        Id = id;
        ContentId = contentId;
        ProjectId = projectId;
        EnvironmentId = environmentId;
        RequestedBy = requestedBy;
        Status = PublicationRequestStatus.Pending;
        RequestedAt = DateTime.UtcNow;
    }

    public static PublicationRequest Create(Guid contentId, Guid projectId, Guid environmentId, Guid requestedBy)
    {
        return new PublicationRequest(Guid.NewGuid(), contentId, projectId, environmentId, requestedBy);
    }

    public void Approve(Guid resolvedBy)
    {
        if (Status != PublicationRequestStatus.Pending)
            throw new InvalidOperationException($"Cannot approve request in status {Status}.");
        Status = PublicationRequestStatus.Approved;
        ResolvedAt = DateTime.UtcNow;
        ResolvedBy = resolvedBy;
    }

    public void Reject(Guid resolvedBy, string? reason = null)
    {
        if (Status != PublicationRequestStatus.Pending)
            throw new InvalidOperationException($"Cannot reject request in status {Status}.");
        Status = PublicationRequestStatus.Rejected;
        ResolvedAt = DateTime.UtcNow;
        ResolvedBy = resolvedBy;
        RejectionReason = reason?.Trim();
    }

    public void SetValidationErrors(string? errors)
    {
        ValidationErrors = errors?.Trim();
    }
}

public enum PublicationRequestStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}
