using IODA.Core.Application.DTOs;
using MediatR;

namespace IODA.Core.Application.Commands.Hierarchies;

public record CreateHierarchyCommand(
    Guid ProjectId,
    string Name,
    string Slug,
    string? Description = null,
    string? ImageUrl = null,
    Guid? ParentHierarchyId = null) : IRequest<HierarchyDto>;
