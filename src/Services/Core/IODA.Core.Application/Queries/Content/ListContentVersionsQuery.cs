using IODA.Core.Application.DTOs;
using MediatR;

namespace IODA.Core.Application.Queries.Content;

public record ListContentVersionsQuery(Guid ContentId) : IRequest<IReadOnlyList<ContentVersionDto>>;
