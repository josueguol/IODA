using IODA.Shared.BuildingBlocks.Domain;

namespace IODA.Identity.Domain.Exceptions;

public class UserAlreadyExistsException : DomainException
{
    public string Email { get; }

    public UserAlreadyExistsException(string email)
        : base($"A user with email '{email}' already exists.")
    {
        Email = email;
    }
}
