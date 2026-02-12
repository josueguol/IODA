using IODA.Core.Application.DTOs;
using MediatR;

namespace IODA.Core.Application.Queries.Sites;

public record ListSitesByProjectAndEnvironmentQuery(Guid ProjectId, Guid EnvironmentId) : IRequest<IReadOnlyList<SiteDto>>;
