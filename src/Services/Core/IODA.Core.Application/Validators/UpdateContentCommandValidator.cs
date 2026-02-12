using FluentValidation;
using IODA.Core.Application.Commands.Content;

namespace IODA.Core.Application.Validators;

public class UpdateContentCommandValidator : AbstractValidator<UpdateContentCommand>
{
    public UpdateContentCommandValidator()
    {
        RuleFor(x => x.ContentId)
            .NotEmpty().WithMessage("ContentId is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Content title is required.")
            .MaximumLength(500).WithMessage("Title must not exceed 500 characters.");

        RuleFor(x => x.Fields)
            .NotNull().WithMessage("Fields cannot be null.");

        RuleFor(x => x.UpdatedBy)
            .NotEmpty().WithMessage("UpdatedBy is required.");
    }
}
