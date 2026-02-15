using FluentValidation;
using IODA.Core.Application.Queries.Projects;

namespace IODA.Core.Application.Validators;

/// <summary>
/// Valida parámetros de paginación de GET /api/projects.
/// Los fallos de autorización (falta de claim project.edit) devuelven 403; los parámetros inválidos devuelven 400.
/// </summary>
public class GetProjectsPagedQueryValidator : AbstractValidator<GetProjectsPagedQuery>
{
    public const int MAX_PAGE_SIZE = 100;

    public GetProjectsPagedQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, MAX_PAGE_SIZE).WithMessage($"PageSize must be between 1 and {MAX_PAGE_SIZE}.");
    }
}
