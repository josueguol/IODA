using MediatR;

namespace IODA.Core.Application.Commands.Sites;

public record DeactivateSiteCommand(Guid SiteId) : IRequest;
