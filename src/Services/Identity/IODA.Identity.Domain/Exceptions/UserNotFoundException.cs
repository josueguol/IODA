using IODA.Shared.BuildingBlocks.Domain;

namespace IODA.Identity.Domain.Exceptions;

public class UserNotFoundException : DomainException
{
    public string? Email { get; }
    public Guid? UserId { get; }

    public UserNotFoundException(string email)
        : base($"User with email '{email}' was not found.")
    {
        Email = email;
    }

    public UserNotFoundException(Guid userId)
        : base($"User with ID '{userId}' was not found.")
    {
        UserId = userId;
    }
}
