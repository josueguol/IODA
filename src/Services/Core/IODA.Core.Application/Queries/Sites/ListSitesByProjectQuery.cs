using IODA.Core.Application.DTOs;
using MediatR;

namespace IODA.Core.Application.Queries.Sites;

public record ListSitesByProjectQuery(Guid ProjectId) : IRequest<IReadOnlyList<SiteDto>>;
