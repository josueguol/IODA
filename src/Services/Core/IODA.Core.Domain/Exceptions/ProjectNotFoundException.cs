using IODA.Shared.BuildingBlocks.Domain;

namespace IODA.Core.Domain.Exceptions;

public class ProjectNotFoundException : DomainException
{
    public Guid ProjectId { get; }

    public ProjectNotFoundException(Guid projectId)
        : base($"Project with ID '{projectId}' was not found.")
    {
        ProjectId = projectId;
    }
}
