using MediatR;

namespace IODA.Publishing.Application.Commands;

public record RequestPublicationCommand(
    Guid ContentId,
    Guid ProjectId,
    Guid EnvironmentId,
    Guid RequestedBy
) : IRequest<Guid>;
