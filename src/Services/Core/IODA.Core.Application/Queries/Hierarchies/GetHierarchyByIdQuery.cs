using IODA.Core.Application.DTOs;
using MediatR;

namespace IODA.Core.Application.Queries.Hierarchies;

public record GetHierarchyByIdQuery(Guid HierarchyId) : IRequest<HierarchyDto?>;
