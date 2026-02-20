namespace IODA.Core.Application.DTOs;

public record HierarchyDto(
    Guid Id,
    Guid ProjectId,
    string Name,
    string Slug,
    string? Description,
    string? ImageUrl,
    Guid? ParentHierarchyId);
