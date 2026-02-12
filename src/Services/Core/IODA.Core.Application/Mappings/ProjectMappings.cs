using IODA.Core.Application.DTOs;
using IODA.Core.Domain.Entities;

namespace IODA.Core.Application.Mappings;

public static class ProjectMappings
{
    public static ProjectDto ToDto(this Project project)
    {
        return new ProjectDto(
            project.Id,
            project.PublicId.FullId,
            project.Name,
            project.Slug.Value,
            project.Description,
            project.IsActive,
            project.CreatedAt,
            project.UpdatedAt,
            project.CreatedBy);
    }
}
