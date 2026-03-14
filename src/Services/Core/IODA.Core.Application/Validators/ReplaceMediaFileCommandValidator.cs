using FluentValidation;
using IODA.Core.Application.Commands.Media;

namespace IODA.Core.Application.Validators;

public class ReplaceMediaFileCommandValidator : AbstractValidator<ReplaceMediaFileCommand>
{
    private const long MaxSizeBytes = 50L * 1024 * 1024; // 50 MB

    public ReplaceMediaFileCommandValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty().WithMessage("ProjectId is required.");

        RuleFor(x => x.MediaItemId)
            .NotEmpty().WithMessage("MediaItemId is required.");

        RuleFor(x => x.UpdatedBy)
            .NotEmpty().WithMessage("UpdatedBy is required.");

        RuleFor(x => x.FileStream)
            .NotNull().WithMessage("File stream is required.");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("FileName is required.");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("ContentType is required.");

        RuleFor(x => x.SizeBytes)
            .GreaterThan(0).WithMessage("File size must be greater than 0.")
            .LessThanOrEqualTo(MaxSizeBytes).WithMessage($"File size must not exceed {MaxSizeBytes / (1024 * 1024)} MB.");

        RuleFor(x => x.DisplayName)
            .MaximumLength(200)
            .When(x => x.DisplayName != null)
            .WithMessage("DisplayName max length is 200 characters.");
    }
}
