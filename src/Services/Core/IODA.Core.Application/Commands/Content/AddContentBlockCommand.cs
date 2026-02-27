using MediatR;

namespace IODA.Core.Application.Commands.Content;

public record AddContentBlockCommand(
    Guid ContentId,
    string BlockType,
    int Order,
    Dictionary<string, object>? Payload) : IRequest<Guid>;
