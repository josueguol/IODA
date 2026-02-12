using FluentValidation;
using IODA.Authorization.Application.Commands;

namespace IODA.Authorization.Application.Validators;

public class CreateAccessRuleCommandValidator : AbstractValidator<CreateAccessRuleCommand>
{
    public CreateAccessRuleCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.ContentStatus).MaximumLength(64).When(x => !string.IsNullOrEmpty(x.ContentStatus));
    }
}
