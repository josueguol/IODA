using IODA.Core.Application.DTOs;
using MediatR;

namespace IODA.Core.Application.Queries.Projects;

public record GetProjectsPagedQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResultDto<ProjectDto>>;
