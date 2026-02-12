namespace IODA.Identity.Application.Interfaces;

/// <summary>
/// Abstracción para hashear y verificar contraseñas.
/// </summary>
public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}
