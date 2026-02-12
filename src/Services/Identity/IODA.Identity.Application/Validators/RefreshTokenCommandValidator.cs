using FluentValidation;

namespace IODA.Identity.Application.Validators;

public class RefreshTokenCommandValidator : AbstractValidator<Commands.RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.");
    }
}
