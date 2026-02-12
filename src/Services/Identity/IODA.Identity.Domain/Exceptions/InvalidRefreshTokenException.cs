using IODA.Shared.BuildingBlocks.Domain;

namespace IODA.Identity.Domain.Exceptions;

public class InvalidRefreshTokenException : DomainException
{
    public InvalidRefreshTokenException()
        : base("Invalid or expired refresh token.")
    {
    }
}
