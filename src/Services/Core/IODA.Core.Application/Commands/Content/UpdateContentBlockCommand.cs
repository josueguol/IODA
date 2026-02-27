using MediatR;

namespace IODA.Core.Application.Commands.Content;

public record UpdateContentBlockCommand(
    Guid BlockId,
    Dictionary<string, object>? Payload,
    int? Order) : IRequest<Unit>;
