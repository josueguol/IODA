using IODA.Shared.BuildingBlocks.Domain;

namespace IODA.Publishing.Domain.Exceptions;

public class PublicationRequestNotFoundException : DomainException
{
    public Guid PublicationRequestId { get; }

    public PublicationRequestNotFoundException(Guid publicationRequestId)
        : base($"Publication request with id '{publicationRequestId}' was not found.")
    {
        PublicationRequestId = publicationRequestId;
    }
}
