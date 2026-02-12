using FluentValidation;

namespace IODA.Publishing.Application.Validators;

public class RequestPublicationCommandValidator : AbstractValidator<Commands.RequestPublicationCommand>
{
    public RequestPublicationCommandValidator()
    {
        RuleFor(x => x.ContentId).NotEmpty();
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.EnvironmentId).NotEmpty();
        RuleFor(x => x.RequestedBy).NotEmpty();
    }
}
