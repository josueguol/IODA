using IODA.Identity.Application.DTOs;
using IODA.Identity.Application.Interfaces;
using IODA.Identity.Domain.Repositories;
using MediatR;

namespace IODA.Identity.Application.Queries;

public class GetSetupStatusQueryHandler : IRequestHandler<GetSetupStatusQuery, SetupStatusDto>
{
    private readonly IUserRepository _userRepository;
    private readonly ISetupConfiguration _setupConfiguration;

    public GetSetupStatusQueryHandler(IUserRepository userRepository, ISetupConfiguration setupConfiguration)
    {
        _userRepository = userRepository;
        _setupConfiguration = setupConfiguration;
    }

    public async Task<SetupStatusDto> Handle(GetSetupStatusQuery request, CancellationToken cancellationToken)
    {
        var hasUsers = await _userRepository.AnyAsync(cancellationToken);
        var selfRegistrationEnabled = _setupConfiguration.SelfRegistrationEnabled;
        return new SetupStatusDto(hasUsers, selfRegistrationEnabled);
    }
}
