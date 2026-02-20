using IODA.Core.Application.DTOs;
using MediatR;

namespace IODA.Core.Application.Commands.Hierarchies;

public record UpdateHierarchyCommand(
    Guid HierarchyId,
    string Name,
    string Slug,
    string? Description = null,
    string? ImageUrl = null,
    Guid? ParentHierarchyId = null) : IRequest<HierarchyDto>;
