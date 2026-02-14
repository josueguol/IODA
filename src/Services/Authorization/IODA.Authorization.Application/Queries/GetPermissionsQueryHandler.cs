using IODA.Authorization.Application.Permissions;
using IODA.Authorization.Domain.Repositories;
using MediatR;

namespace IODA.Authorization.Application.Queries;

public class GetPermissionsQueryHandler : IRequestHandler<GetPermissionsQuery, IReadOnlyList<PermissionDto>>
{
    private readonly IPermissionRepository _permissionRepository;

    public GetPermissionsQueryHandler(IPermissionRepository permissionRepository)
    {
        _permissionRepository = permissionRepository;
    }

    public async Task<IReadOnlyList<PermissionDto>> Handle(GetPermissionsQuery request, CancellationToken cancellationToken)
    {
        var permissions = await _permissionRepository.GetAllAsync(cancellationToken);
        var catalogCodes = PermissionCatalog.AllCodes;
        return permissions
            .Where(p => catalogCodes.Contains(p.Code))
            .Select(p => new PermissionDto(p.Id, p.Code, p.Description))
            .OrderBy(p => p.Code)
            .ToList();
    }
}
