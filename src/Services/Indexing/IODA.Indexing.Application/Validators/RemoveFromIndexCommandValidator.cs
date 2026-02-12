using FluentValidation;
using IODA.Indexing.Application.Commands;

namespace IODA.Indexing.Application.Validators;

public class RemoveFromIndexCommandValidator : AbstractValidator<RemoveFromIndexCommand>
{
    public RemoveFromIndexCommandValidator()
    {
        RuleFor(x => x.ContentId).NotEmpty();
    }
}
