using IODA.Authorization.Domain.Entities;
using IODA.Authorization.Domain.Exceptions;
using IODA.Authorization.Domain.Repositories;
using MediatR;

namespace IODA.Authorization.Application.Commands;

public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, Guid>
{
    private readonly IRoleRepository _roleRepository;

    public CreateRoleCommandHandler(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<Guid> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        if (await _roleRepository.ExistsByNameAsync(request.Name, null, cancellationToken))
            throw new InvalidOperationException($"Role with name '{request.Name}' already exists.");
        var role = Role.Create(request.Name, request.Description);
        await _roleRepository.AddAsync(role, cancellationToken);
        return role.Id;
    }
}
