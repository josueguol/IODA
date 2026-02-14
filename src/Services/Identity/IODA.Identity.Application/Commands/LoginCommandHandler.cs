using IODA.Identity.Application.DTOs;
using IODA.Identity.Application.Interfaces;
using IODA.Identity.Domain.Entities;
using IODA.Identity.Domain.Exceptions;
using IODA.Identity.Domain.Repositories;
using MediatR;

namespace IODA.Identity.Application.Commands;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResultDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly IAuthEventPublisher _authEventPublisher;
    private readonly IEffectivePermissionsClient _effectivePermissionsClient;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenGenerator refreshTokenGenerator,
        IAuthEventPublisher authEventPublisher,
        IEffectivePermissionsClient effectivePermissionsClient)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenGenerator = refreshTokenGenerator;
        _authEventPublisher = authEventPublisher;
        _effectivePermissionsClient = effectivePermissionsClient;
    }

    public async Task<LoginResultDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (user is null)
            throw new InvalidCredentialsException();

        if (!user.IsActive)
            throw new InvalidCredentialsException();

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            throw new InvalidCredentialsException();

        user.RecordLogin();
        await _userRepository.UpdateAsync(user, cancellationToken);

        var effectivePermissions = await _effectivePermissionsClient.GetEffectivePermissionsAsync(user.Id, cancellationToken);
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email);
        var expiresInSeconds = _jwtTokenGenerator.GetAccessTokenExpirationMinutes() * 60;

        var (refreshTokenValue, refreshTokenValidity) = _refreshTokenGenerator.Generate();
        var refreshTokenEntity = RefreshToken.Create(user.Id, refreshTokenValue, refreshTokenValidity);
        await _refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);

        await _authEventPublisher.PublishUserLoggedInAsync(user.Id, user.Email, DateTime.UtcNow, cancellationToken);

        return new LoginResultDto(
            accessToken,
            refreshTokenValue,
            expiresInSeconds,
            user.Id,
            user.Email,
            user.DisplayName);
    }
}
