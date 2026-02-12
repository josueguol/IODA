using IODA.Identity.Application.Interfaces;

namespace IODA.Identity.Infrastructure.Services;

public class BCryptPasswordHasher : IPasswordHasher
{
    private static readonly int WorkFactor = 12;

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
