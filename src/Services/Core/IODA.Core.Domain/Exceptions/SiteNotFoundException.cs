using IODA.Shared.BuildingBlocks.Domain;

namespace IODA.Core.Domain.Exceptions;

public class SiteNotFoundException : DomainException
{
    public Guid SiteId { get; }

    public SiteNotFoundException(Guid siteId)
        : base($"Site with ID '{siteId}' was not found.")
    {
        SiteId = siteId;
    }
}
