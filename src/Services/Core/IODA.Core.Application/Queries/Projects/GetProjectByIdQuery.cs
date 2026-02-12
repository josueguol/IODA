using IODA.Core.Application.DTOs;
using MediatR;

namespace IODA.Core.Application.Queries.Projects;

public record GetProjectByIdQuery(Guid ProjectId) : IRequest<ProjectDto?>;
