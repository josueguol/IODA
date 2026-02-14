using IODA.Identity.Application.DTOs;
using IODA.Identity.Application.Interfaces;
using IODA.Identity.Domain.Entities;
using IODA.Identity.Domain.Exceptions;
using IODA.Identity.Domain.Repositories;
using MediatR;

namespace IODA.Identity.Application.Commands;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResultDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISetupConfiguration _setupConfiguration;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ISetupConfiguration setupConfiguration)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _setupConfiguration = setupConfiguration;
    }

    public async Task<RegisterResultDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var hasUsers = await _userRepository.AnyAsync(cancellationToken);
        if (hasUsers && !_setupConfiguration.SelfRegistrationEnabled)
            throw new SelfRegistrationDisabledException();

        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        if (await _userRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken))
            throw new UserAlreadyExistsException(request.Email);

        var passwordHash = _passwordHasher.HashPassword(request.Password);
        var user = User.Create(request.Email.Trim(), passwordHash, request.DisplayName);
        await _userRepository.AddAsync(user, cancellationToken);
        return new RegisterResultDto(user.Id, !hasUsers);
    }
}
