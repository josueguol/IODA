using IODA.Core.Application.DTOs;
using MediatR;

namespace IODA.Core.Application.Queries.Tags;

public record ListTagsByProjectQuery(Guid ProjectId) : IRequest<IReadOnlyList<TagDto>>;
