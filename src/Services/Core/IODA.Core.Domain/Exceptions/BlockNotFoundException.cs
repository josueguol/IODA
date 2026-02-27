using IODA.Shared.BuildingBlocks.Domain;

namespace IODA.Core.Domain.Exceptions;

public class BlockNotFoundException : DomainException
{
    public Guid BlockId { get; }

    public BlockNotFoundException(Guid blockId)
        : base($"Block with ID '{blockId}' was not found.")
    {
        BlockId = blockId;
    }
}
