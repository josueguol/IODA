using IODA.Core.Application.Interfaces;
using IODA.Core.Domain.Entities;
using IODA.Core.Domain.Exceptions;
using IODA.Core.Domain.Repositories;
using MediatR;

namespace IODA.Core.Application.Commands.Content;

public class AddContentBlockCommandHandler : IRequestHandler<AddContentBlockCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBlockAllowedBySchemaValidator _blockValidator;

    public AddContentBlockCommandHandler(IUnitOfWork unitOfWork, IBlockAllowedBySchemaValidator blockValidator)
    {
        _unitOfWork = unitOfWork;
        _blockValidator = blockValidator;
    }

    public async Task<Guid> Handle(AddContentBlockCommand request, CancellationToken cancellationToken)
    {
        var content = await _unitOfWork.Contents.GetByIdAsync(request.ContentId, cancellationToken);
        if (content == null)
            throw new ContentNotFoundException(request.ContentId);

        var schema = await _unitOfWork.Schemas.GetByIdAsync(content.SchemaId, cancellationToken);
        if (schema == null)
            throw new SchemaNotFoundException(content.SchemaId);

        _blockValidator.ValidateAdd(content, schema, request.BlockType);

        var block = ContentBlock.Create(
            request.ContentId,
            request.BlockType,
            request.Order,
            request.Payload);
        content.AddBlock(block);
        await _unitOfWork.Contents.UpdateAsync(content, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return block.Id;
    }
}
