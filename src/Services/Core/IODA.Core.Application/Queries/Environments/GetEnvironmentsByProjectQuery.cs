using IODA.Core.Application.DTOs;
using MediatR;

namespace IODA.Core.Application.Queries.Environments;

public record GetEnvironmentsByProjectQuery(Guid ProjectId) : IRequest<IReadOnlyList<EnvironmentDto>>;
