using FluentValidation;
using IODA.Core.Application.Commands.Content;

namespace IODA.Core.Application.Validators;

public class PublishContentCommandValidator : AbstractValidator<PublishContentCommand>
{
    public PublishContentCommandValidator()
    {
        RuleFor(x => x.ContentId)
            .NotEmpty().WithMessage("ContentId is required.");

        RuleFor(x => x.PublishedBy)
            .NotEmpty().WithMessage("PublishedBy is required.");
    }
}
