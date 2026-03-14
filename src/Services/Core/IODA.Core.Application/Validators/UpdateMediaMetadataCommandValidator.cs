using FluentValidation;
using IODA.Core.Application.Commands.Media;

namespace IODA.Core.Application.Validators;

public class UpdateMediaMetadataCommandValidator : AbstractValidator<UpdateMediaMetadataCommand>
{
    public UpdateMediaMetadataCommandValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty().WithMessage("ProjectId is required.");

        RuleFor(x => x.MediaItemId)
            .NotEmpty().WithMessage("MediaItemId is required.");

        RuleFor(x => x.UpdatedBy)
            .NotEmpty().WithMessage("UpdatedBy is required.");

        RuleFor(x => x.DisplayName)
            .MaximumLength(200)
            .When(x => x.DisplayName != null)
            .WithMessage("DisplayName max length is 200 characters.");
    }
}
