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
            field.RuleFor(f => f.Label)
                .NotEmpty().WithMessage("Field label is required.")
                .MaximumLength(200).WithMessage("Field label must not exceed 200 characters.");

            field.RuleFor(f => f.Slug)
                .NotEmpty().WithMessage("Field slug is required.")
                .MaximumLength(100).WithMessage("Field slug must not exceed 100 characters.")
                .Matches("^[a-z0-9]+(-[a-z0-9]+)*$").WithMessage("Field slug must be kebab-case (lowercase letters, numbers, hyphens only).");

            field.RuleFor(f => f.FieldType)
                .NotEmpty().WithMessage("Field type is required.")
                .MaximumLength(50).WithMessage("Field type must not exceed 50 characters.");
        });

        RuleFor(x => x.Fields)
            .Must(fields =>
            {
                var slugs = fields.Where(f => !string.IsNullOrWhiteSpace(f.Slug)).Select(f => f.Slug!.Trim().ToLowerInvariant()).ToList();
                return slugs.Count == slugs.Distinct().Count();
            })
            .WithMessage("Field slugs must be unique within the schema.");

        RuleFor(x => x.CreatedBy)
            .NotEmpty().WithMessage("CreatedBy is required.");
    }
}
