using MediatR;

namespace IODA.Core.Application.Commands.Environments;

public record CreateEnvironmentCommand(
    Guid ProjectId,
    string Name,
    string? Description,
    Guid CreatedBy) : IRequest<Guid>;
