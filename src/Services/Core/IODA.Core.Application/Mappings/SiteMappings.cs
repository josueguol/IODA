using IODA.Core.Application.DTOs;
using IODA.Core.Domain.Entities;

namespace IODA.Core.Application.Mappings;

public static class SiteMappings
{
    public static SiteDto ToDto(this Site site)
    {
        return new SiteDto(
            site.Id,
            site.PublicId.FullId,
            site.ProjectId,
            site.EnvironmentId,
            site.Name,
            site.Domain,
            site.Subdomain,
            site.Subpath,
            site.ThemeId,
            site.IsActive,
            site.CreatedAt,
            site.UpdatedAt,
            site.CreatedBy);
    }

    public static SiteListItemDto ToListItemDto(this Site site)
    {
        return new SiteListItemDto(
            site.Id,
            site.PublicId.FullId,
            site.Name,
            site.Domain,
            site.Subdomain,
            site.IsActive);
    }
}
