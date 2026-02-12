using MediatR;

namespace IODA.Core.Application.Commands.Sites;

public record ActivateSiteCommand(Guid SiteId) : IRequest;
