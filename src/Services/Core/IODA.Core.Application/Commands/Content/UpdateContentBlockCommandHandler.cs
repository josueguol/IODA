using IODA.Core.Domain.Exceptions;
using IODA.Core.Domain.Repositories;
using MediatR;

namespace IODA.Core.Application.Commands.Content;

public class UpdateContentBlockCommandHandler : IRequestHandler<UpdateContentBlockCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateContentBlockCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(UpdateContentBlockCommand request, CancellationToken cancellationToken)
    {
        var content = await _unitOfWork.Contents.GetByBlockIdAsync(request.BlockId, cancellationToken);
        if (content == null)
            throw new BlockNotFoundException(request.BlockId);

        var block = content.Blocks.FirstOrDefault(b => b.Id == request.BlockId);
        if (block == null)
            throw new BlockNotFoundException(request.BlockId);

        if (request.Payload != null)
            block.UpdatePayload(request.Payload);
        if (request.Order.HasValue)
            block.SetOrder(request.Order.Value);

        await _unitOfWork.Contents.UpdateAsync(content, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
