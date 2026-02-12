using IODA.Core.Application.DTOs;
using MediatR;

namespace IODA.Core.Application.Queries.Sites;

public record GetSiteByIdQuery(Guid SiteId) : IRequest<SiteDto?>;
