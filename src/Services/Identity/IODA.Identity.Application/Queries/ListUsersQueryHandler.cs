using IODA.Identity.Application.DTOs;
using IODA.Identity.Domain.Repositories;
using MediatR;

namespace IODA.Identity.Application.Queries;

public class ListUsersQueryHandler : IRequestHandler<ListUsersQuery, IReadOnlyList<UserListItemDto>>
{
    private readonly IUserRepository _userRepository;

    public ListUsersQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<IReadOnlyList<UserListItemDto>> Handle(ListUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        return users
            .Select(u => new UserListItemDto(u.Id, u.Email, u.DisplayName, u.IsActive, u.CreatedAt))
            .ToList();
    }
}
