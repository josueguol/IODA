using System.Security.Cryptography;
using IODA.Identity.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace IODA.Identity.Infrastructure.Services;

public class RefreshTokenGenerator : IRefreshTokenGenerator
{
    private readonly TimeSpan _validity;

    public RefreshTokenGenerator(IConfiguration configuration)
    {
        var days = int.TryParse(configuration["Jwt:RefreshTokenExpirationDays"], out var d) ? d : 7;
        _validity = TimeSpan.FromDays(days);
    }

    public (string Token, TimeSpan Validity) Generate()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var token = Convert.ToBase64String(randomBytes);
        return (token, _validity);
    }
}
