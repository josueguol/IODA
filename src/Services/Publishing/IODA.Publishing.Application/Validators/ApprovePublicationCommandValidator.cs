using FluentValidation;
using IODA.Publishing.Application.Commands;

namespace IODA.Publishing.Application.Validators;

public class ApprovePublicationCommandValidator : AbstractValidator<ApprovePublicationCommand>
{
    public ApprovePublicationCommandValidator()
    {
        RuleFor(x => x.PublicationRequestId).NotEmpty();
        RuleFor(x => x.ApprovedBy).NotEmpty();
    }
}
