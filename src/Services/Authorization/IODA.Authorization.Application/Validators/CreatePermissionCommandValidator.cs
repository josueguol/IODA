using FluentValidation;
using IODA.Authorization.Application.Commands;

namespace IODA.Authorization.Application.Validators;

public class CreatePermissionCommandValidator : AbstractValidator<CreatePermissionCommand>
{
    public CreatePermissionCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}
