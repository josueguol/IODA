using MediatR;

namespace IODA.Core.Application.Commands.Sites;

public record CreateSiteCommand(
    Guid ProjectId,
    Guid? EnvironmentId,
    string Name,
    string Domain,
    string? Subdomain = null,
    string? Subpath = null,
    string? ThemeId = null,
    string? UrlTemplate = null,
    Guid CreatedBy = default) : IRequest<Guid>;
