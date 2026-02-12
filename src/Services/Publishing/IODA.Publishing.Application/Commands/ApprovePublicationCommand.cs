using MediatR;

namespace IODA.Publishing.Application.Commands;

public record ApprovePublicationCommand(Guid PublicationRequestId, Guid ApprovedBy) : IRequest;
