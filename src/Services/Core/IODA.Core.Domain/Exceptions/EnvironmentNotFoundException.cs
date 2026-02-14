using IODA.Shared.BuildingBlocks.Domain;

namespace IODA.Core.Domain.Exceptions;

public class EnvironmentNotFoundException : DomainException
{
    public Guid EnvironmentId { get; }

    public EnvironmentNotFoundException(Guid environmentId)
        : base($"Environment with ID '{environmentId}' was not found.")
    {
        EnvironmentId = environmentId;
    }
}
