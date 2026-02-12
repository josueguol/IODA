using FluentValidation;
using IODA.Core.Application.Commands.Content;

namespace IODA.Core.Application.Validators;

public class CreateContentCommandValidator : AbstractValidator<CreateContentCommand>
{
    public CreateContentCommandValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty().WithMessage("ProjectId is required.");

        RuleFor(x => x.EnvironmentId)
            .NotEmpty().WithMessage("EnvironmentId is required.");

        RuleFor(x => x.SchemaId)
            .NotEmpty().WithMessage("SchemaId is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Content title is required.")
            .MaximumLength(500).WithMessage("Title must not exceed 500 characters.");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("ContentType is required.")
            .MaximumLength(100).WithMessage("ContentType must not exceed 100 characters.");

        RuleFor(x => x.Fields)
            .NotNull().WithMessage("Fields cannot be null.");

        RuleFor(x => x.CreatedBy)
            .NotEmpty().WithMessage("CreatedBy is required.");
    }
}
