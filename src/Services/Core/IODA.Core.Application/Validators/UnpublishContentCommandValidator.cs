using FluentValidation;
using IODA.Core.Application.Commands.Content;

namespace IODA.Core.Application.Validators;

public class UnpublishContentCommandValidator : AbstractValidator<UnpublishContentCommand>
{
    public UnpublishContentCommandValidator()
    {
        RuleFor(x => x.ContentId)
            .NotEmpty().WithMessage("ContentId is required.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required.")
            .MaximumLength(500).WithMessage("Reason must not exceed 500 characters.");

        RuleFor(x => x.UnpublishedBy)
            .NotEmpty().WithMessage("UnpublishedBy is required.");
    }
}
