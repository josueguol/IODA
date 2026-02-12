using IODA.Shared.BuildingBlocks.Domain;

namespace IODA.Authorization.Domain.Exceptions;

public class PermissionNotFoundException : DomainException
{
    public Guid PermissionId { get; }
    public string? Code { get; }

    public PermissionNotFoundException(Guid permissionId)
        : base($"Permission with id '{permissionId}' was not found.")
    {
        PermissionId = permissionId;
    }

    public PermissionNotFoundException(string code)
        : base($"Permission with code '{code}' was not found.")
    {
        Code = code;
    }
}
