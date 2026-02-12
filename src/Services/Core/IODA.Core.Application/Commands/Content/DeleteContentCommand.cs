using MediatR;

namespace IODA.Core.Application.Commands.Content;

public record DeleteContentCommand(Guid ContentId) : IRequest<Unit>;
