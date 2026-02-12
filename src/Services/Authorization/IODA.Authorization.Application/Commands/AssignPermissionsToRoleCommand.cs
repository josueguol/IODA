using MediatR;

namespace IODA.Authorization.Application.Commands;

public record AssignPermissionsToRoleCommand(Guid RoleId, IReadOnlyList<Guid> PermissionIds) : IRequest;
