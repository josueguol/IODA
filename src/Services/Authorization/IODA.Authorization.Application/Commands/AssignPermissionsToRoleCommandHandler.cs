using IODA.Authorization.Application.Permissions;
using IODA.Authorization.Domain.Exceptions;
using IODA.Authorization.Domain.Repositories;
using MediatR;

namespace IODA.Authorization.Application.Commands;

public class AssignPermissionsToRoleCommandHandler : IRequestHandler<AssignPermissionsToRoleCommand>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;

    public AssignPermissionsToRoleCommandHandler(IRoleRepository roleRepository, IPermissionRepository permissionRepository)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
    }

    public async Task Handle(AssignPermissionsToRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdWithPermissionsAsync(request.RoleId, cancellationToken)
            ?? throw new RoleNotFoundException(request.RoleId);

        foreach (var permissionId in request.PermissionIds)
        {
            var permission = await _permissionRepository.GetByIdAsync(permissionId, cancellationToken);
            if (permission == null)
                throw new ArgumentException($"Permission with id '{permissionId}' was not found.");
            if (!PermissionCatalog.IsInCatalog(permission.Code))
                throw new ArgumentException($"Permission '{permission.Code}' (id: {permissionId}) is not in the system catalog and cannot be assigned to roles.");
            role.GrantPermission(permissionId);
        }

        await _roleRepository.UpdateAsync(role, cancellationToken);
    }
}
