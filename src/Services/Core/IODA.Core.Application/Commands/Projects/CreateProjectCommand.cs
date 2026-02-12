using MediatR;

namespace IODA.Core.Application.Commands.Projects;

public record CreateProjectCommand(
    string Name,
    string? Description,
    Guid CreatedBy) : IRequest<Guid>;
