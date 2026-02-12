using IODA.Core.Application.DTOs;
using EnvironmentEntity = IODA.Core.Domain.Entities.Environment;

namespace IODA.Core.Application.Mappings;

public static class EnvironmentMappings
{
    public static EnvironmentDto ToDto(this EnvironmentEntity environment)
    {
        return new EnvironmentDto(
            environment.Id,
            environment.PublicId.FullId,
            environment.Name,
            environment.Slug.Value,
            environment.Description,
            environment.IsActive,
            environment.ProjectId,
            environment.CreatedAt,
            environment.UpdatedAt);
    }
}
