using IODA.Shared.BuildingBlocks.Domain;

namespace IODA.Identity.Domain.Exceptions;

/// <summary>Se lanza cuando el auto-registro está deshabilitado y el usuario no es el primero.</summary>
public class SelfRegistrationDisabledException : DomainException
{
    public SelfRegistrationDisabledException()
        : base("Self-registration is disabled. Contact an administrator.")
    {
    }
}
