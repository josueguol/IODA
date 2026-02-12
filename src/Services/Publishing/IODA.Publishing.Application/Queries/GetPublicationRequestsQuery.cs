using IODA.Publishing.Domain.Entities;
using MediatR;

namespace IODA.Publishing.Application.Queries;

public record GetPublicationRequestsQuery(
    Guid? ContentId = null,
    PublicationRequestStatus? Status = null
) : IRequest<IReadOnlyList<PublicationRequestDto>>;

public record PublicationRequestDto(
    Guid Id,
    Guid ContentId,
    Guid ProjectId,
    Guid EnvironmentId,
    Guid RequestedBy,
    PublicationRequestStatus Status,
    DateTime RequestedAt,
    DateTime? ResolvedAt,
    Guid? ResolvedBy,
    string? RejectionReason,
    string? ValidationErrors
);
