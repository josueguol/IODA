using FluentValidation;
using IODA.Publishing.Application.Commands;

namespace IODA.Publishing.Application.Validators;

public class RejectPublicationCommandValidator : AbstractValidator<RejectPublicationCommand>
{
    public RejectPublicationCommandValidator()
    {
        RuleFor(x => x.PublicationRequestId).NotEmpty();
        RuleFor(x => x.RejectedBy).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(2000).When(x => !string.IsNullOrEmpty(x.Reason));
    }
}
