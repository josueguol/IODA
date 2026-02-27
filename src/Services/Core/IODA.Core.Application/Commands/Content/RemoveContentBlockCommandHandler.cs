using IODA.Core.Application.Interfaces;
using IODA.Core.Domain.Exceptions;
using IODA.Core.Domain.Repositories;
using MediatR;

namespace IODA.Core.Application.Commands.Content;

public class RemoveContentBlockCommandHandler : IRequestHandler<RemoveContentBlockCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBlockAllowedBySchemaValidator _blockValidator;

    public RemoveContentBlockCommandHandler(IUnitOfWork unitOfWork, IBlockAllowedBySchemaValidator blockValidator)
    {
        _unitOfWork = unitOfWork;
        _blockValidator = blockValidator;
    }

    public async Task<Unit> Handle(RemoveContentBlockCommand request, CancellationToken cancellationToken)
    {
        var content = await _unitOfWork.Contents.GetByBlockIdAsync(request.BlockId, cancellationToken);
        if (content == null)
            throw new BlockNotFoundException(request.BlockId);

        var block = content.Blocks.FirstOrDefault(b => b.Id == request.BlockId);
        if (block != null)
        {
            var schema = await _unitOfWork.Schemas.GetByIdAsync(content.SchemaId, cancellationToken);
            if (schema != null)
                _blockValidator.ValidateRemove(content, schema, block.BlockType);
        }

        content.RemoveBlock(request.BlockId);
        await _unitOfWork.Contents.UpdateAsync(content, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
