using MediatR;

namespace IODA.Publishing.Application.Commands;

public record RejectPublicationCommand(Guid PublicationRequestId, Guid RejectedBy, string? Reason = null) : IRequest;
