using IODA.Core.Application.DTOs;
using MediatR;

namespace IODA.Core.Application.Queries.Hierarchies;

public record ListHierarchiesByProjectQuery(Guid ProjectId) : IRequest<IReadOnlyList<HierarchyDto>>;
