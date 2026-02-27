using IODA.Core.Domain.Exceptions;
using IODA.Core.Domain.Repositories;
using MediatR;

namespace IODA.Core.Application.Commands.Content;

public class ReorderContentBlocksCommandHandler : IRequestHandler<ReorderContentBlocksCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;

    public ReorderContentBlocksCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(ReorderContentBlocksCommand request, CancellationToken cancellationToken)
    {
        var content = await _unitOfWork.Contents.GetByIdAsync(request.ContentId, cancellationToken);
        if (content == null)
            throw new ContentNotFoundException(request.ContentId);

        content.ReorderBlocks(request.BlockIdsInOrder);
        await _unitOfWork.Contents.UpdateAsync(content, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
