using IODA.Shared.BuildingBlocks.Domain;

namespace IODA.Core.Domain.Exceptions;

public class MediaItemNotFoundException : DomainException
{
    public Guid MediaItemId { get; }

    public MediaItemNotFoundException(Guid mediaItemId)
        : base($"Media item with ID '{mediaItemId}' was not found.")
    {
        MediaItemId = mediaItemId;
    }
}
