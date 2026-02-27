using MediatR;

namespace IODA.Core.Application.Commands.Content;

public record RemoveContentBlockCommand(Guid BlockId) : IRequest<Unit>;
