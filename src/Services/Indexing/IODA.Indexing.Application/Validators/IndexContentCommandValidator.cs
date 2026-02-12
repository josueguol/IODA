using FluentValidation;
using IODA.Indexing.Application.Commands;

namespace IODA.Indexing.Application.Validators;

public class IndexContentCommandValidator : AbstractValidator<IndexContentCommand>
{
    public IndexContentCommandValidator()
    {
        RuleFor(x => x.ContentId).NotEmpty();
        RuleFor(x => x.VersionId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(256);
    }
}
