using IODA.Shared.BuildingBlocks.Domain;

namespace IODA.Core.Domain.Exceptions;

public class ContentNotFoundException : DomainException
{
    public Guid ContentId { get; }

    public ContentNotFoundException(Guid contentId)
        : base($"Content with ID '{contentId}' was not found.")
    {
        ContentId = contentId;
    }
}
