using IODA.Shared.BuildingBlocks.Domain;

namespace IODA.Core.Domain.Exceptions;

/// <summary>
/// Thrown when a block type is not in the content's schema AllowedBlockTypes or violates min/max occurrences.
/// </summary>
public class BlockTypeNotAllowedException : DomainException
{
    public string BlockType { get; }
    public Guid ContentId { get; }

    public BlockTypeNotAllowedException(string blockType, Guid contentId, string message)
        : base(message)
    {
        BlockType = blockType;
        ContentId = contentId;
    }
}
