using IODA.Identity.Application.DTOs;
using IODA.Identity.Application.Interfaces;
using IODA.Identity.Domain.Entities;
using IODA.Identity.Domain.Exceptions;
using IODA.Identity.Domain.Repositories;
using MediatR;

namespace IODA.Identity.Application.Commands;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, LoginResultDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly IEffectivePermissionsClient _effectivePermissionsClient;
    private readonly IUserRolesClient _userRolesClient;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenGenerator refreshTokenGenerator,
        IEffectivePermissionsClient effectivePermissionsClient,
        IUserRolesClient userRolesClient)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenGenerator = refreshTokenGenerator;
        _effectivePermissionsClient = effectivePermissionsClient;
        _userRolesClient = userRolesClient;
    }

    public async Task<LoginResultDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var storedToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);
        if (storedToken is null || !storedToken.IsValid)
            throw new InvalidRefreshTokenException();

        var user = await _userRepository.GetByIdAsync(storedToken.UserId, cancellationToken);
        if (user is null || !user.IsActive)
            throw new InvalidRefreshTokenException();

        storedToken.Revoke();
        await _refreshTokenRepository.UpdateAsync(storedToken, cancellationToken);

        var effectivePermissions = await _effectivePermissionsClient.GetEffectivePermissionsAsync(user.Id, cancellationToken);
        var roleNames = await _userRolesClient.GetRoleNamesAsync(user.Id, cancellationToken);
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email, roles: roleNames, permissionCodes: effectivePermissions);
        var expiresInSeconds = _jwtTokenGenerator.GetAccessTokenExpirationMinutes() * 60;

        var (refreshTokenValue, refreshTokenValidity) = _refreshTokenGenerator.Generate();
        var newRefreshToken = RefreshToken.Create(user.Id, refreshTokenValue, refreshTokenValidity);
        await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);

        return new LoginResultDto(
            accessToken,
            refreshTokenValue,
            expiresInSeconds,
            user.Id,
            user.Email,
            user.DisplayName);
    }
}
