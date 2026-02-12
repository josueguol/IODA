using IODA.Shared.BuildingBlocks.Domain;

namespace IODA.Identity.Domain.Exceptions;

/// <summary>
/// Se lanza cuando el email o la contraseña no son válidos (sin revelar cuál falló).
/// </summary>
public class InvalidCredentialsException : DomainException
{
    public InvalidCredentialsException()
        : base("Invalid email or password.")
    {
    }
}
