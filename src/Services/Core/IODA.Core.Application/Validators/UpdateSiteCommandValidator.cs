using FluentValidation;
using IODA.Core.Application.Commands.Sites;

namespace IODA.Core.Application.Validators;

public class UpdateSiteCommandValidator : AbstractValidator<UpdateSiteCommand>
{
    public UpdateSiteCommandValidator()
    {
        RuleFor(x => x.SiteId)
            .NotEmpty().WithMessage("SiteId is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Site name is required.")
            .MaximumLength(200).WithMessage("Site name must not exceed 200 characters.");

        RuleFor(x => x.Domain)
            .NotEmpty().WithMessage("Domain is required.")
            .MaximumLength(255).WithMessage("Domain must not exceed 255 characters.")
            .Matches(@"^([a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,}$")
            .WithMessage("Domain must be a valid domain name (e.g., example.com).");

        RuleFor(x => x.Subdomain)
            .MaximumLength(255).WithMessage("Subdomain must not exceed 255 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Subdomain));

        RuleFor(x => x.Subpath)
            .MaximumLength(500).WithMessage("Subpath must not exceed 500 characters.")
            .Must(path => string.IsNullOrEmpty(path) || path.StartsWith("/"))
            .WithMessage("Subpath must start with '/' if provided.")
            .When(x => !string.IsNullOrWhiteSpace(x.Subpath));

        RuleFor(x => x.ThemeId)
            .MaximumLength(200).WithMessage("ThemeId must not exceed 200 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.ThemeId));
    }
}
