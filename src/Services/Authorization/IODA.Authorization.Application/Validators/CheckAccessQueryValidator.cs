using FluentValidation;

namespace IODA.Authorization.Application.Validators;

public class CheckAccessQueryValidator : AbstractValidator<Queries.CheckAccessQuery>
{
    public CheckAccessQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.PermissionCode).NotEmpty().MaximumLength(128);
    }
}
