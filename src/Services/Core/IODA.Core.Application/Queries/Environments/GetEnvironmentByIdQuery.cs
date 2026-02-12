using IODA.Core.Application.DTOs;
using MediatR;

namespace IODA.Core.Application.Queries.Environments;

public record GetEnvironmentByIdQuery(Guid ProjectId, Guid EnvironmentId) : IRequest<EnvironmentDto?>;
