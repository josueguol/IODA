using FluentValidation;
using IODA.Core.Application.Commands.Schemas;

namespace IODA.Core.Application.Validators;

public class CreateContentSchemaCommandValidator : AbstractValidator<CreateContentSchemaCommand>
{
    public CreateContentSchemaCommandValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty().WithMessage("ProjectId is required.");

        RuleFor(x => x.SchemaName)
            .NotEmpty().WithMessage("Schema name is required.")
            .MaximumLength(100).WithMessage("Schema name must not exceed 100 characters.");

        RuleFor(x => x.SchemaType)
            .NotEmpty().WithMessage("Schema type is required.")
            .MaximumLength(100).WithMessage("Schema type must not exceed 100 characters.")
            .Matches("^[a-z][a-z0-9_-]*$").WithMessage("Schema type must be lowercase alphanumeric with underscores or hyphens (e.g. article, blog_post, video-article).");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.")
            .When(x => x.Description != null);

        RuleFor(x => x.Fields)
            .NotEmpty().WithMessage("Schema must have at least one field.")
            .Must(f => f.Count > 0).WithMessage("Schema must have at least one field.");

        RuleForEach(x => x.Fields).ChildRules(field =>
        {
            field.RuleFor(f => f.FieldName)
                .NotEmpty().WithMessage("Field name is required.")
                .MaximumLength(100).WithMessage("Field name must not exceed 100 characters.")
                .Matches("^[a-zA-Z][a-zA-Z0-9_]*$").WithMessage("Field name must start with a letter and contain only letters, numbers, and underscores.");

            field.RuleFor(f => f.FieldType)
                .NotEmpty().WithMessage("Field type is required.")
                .MaximumLength(50).WithMessage("Field type must not exceed 50 characters.");
        });

        RuleFor(x => x.CreatedBy)
            .NotEmpty().WithMessage("CreatedBy is required.");
    }
}
