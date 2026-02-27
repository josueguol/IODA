using MediatR;

namespace IODA.Core.Application.Commands.Content;

public record ReorderContentBlocksCommand(
    Guid ContentId,
    IReadOnlyList<Guid> BlockIdsInOrder) : IRequest<Unit>;
