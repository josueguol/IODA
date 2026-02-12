using IODA.Identity.Application.Interfaces;
using IODA.Identity.Domain.Entities;
using IODA.Identity.Domain.Exceptions;
using IODA.Identity.Domain.Repositories;
using MediatR;

namespace IODA.Identity.Application.Commands;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Guid>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Guid> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        if (await _userRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken))
            throw new UserAlreadyExistsException(request.Email);

        var passwordHash = _passwordHasher.HashPassword(request.Password);
        var user = User.Create(request.Email.Trim(), passwordHash, request.DisplayName);
        await _userRepository.AddAsync(user, cancellationToken);
        return user.Id;
    }
}
