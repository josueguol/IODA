using IODA.Authorization.Domain.Entities;
using IODA.Authorization.Domain.Repositories;
using MediatR;

namespace IODA.Authorization.Application.Commands;

public class CreatePermissionCommandHandler : IRequestHandler<CreatePermissionCommand, Guid>
{
    private readonly IPermissionRepository _permissionRepository;

    public CreatePermissionCommandHandler(IPermissionRepository permissionRepository)
    {
        _permissionRepository = permissionRepository;
    }

    public async Task<Guid> Handle(CreatePermissionCommand request, CancellationToken cancellationToken)
    {
        if (await _permissionRepository.ExistsByCodeAsync(request.Code, cancellationToken))
            throw new InvalidOperationException($"Permission with code '{request.Code}' already exists.");
        var permission = Permission.Create(request.Code, request.Description);
        await _permissionRepository.AddAsync(permission, cancellationToken);
        return permission.Id;
    }
}
