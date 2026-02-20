using IODA.Core.Application.DTOs;
using MediatR;

namespace IODA.Core.Application.Commands.Sites;

public record UpdateSiteCommand(
    Guid SiteId,
    string Name,
    string Domain,
    string? Subdomain = null,
    string? Subpath = null,
    string? ThemeId = null,
    string? UrlTemplate = null) : IRequest<SiteDto>;
