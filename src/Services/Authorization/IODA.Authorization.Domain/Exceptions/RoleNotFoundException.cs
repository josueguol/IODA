using IODA.Shared.BuildingBlocks.Domain;

namespace IODA.Authorization.Domain.Exceptions;

public class RoleNotFoundException : DomainException
{
    public Guid RoleId { get; }

    public RoleNotFoundException(Guid roleId)
        : base($"Role with id '{roleId}' was not found.")
    {
        RoleId = roleId;
    }
}
