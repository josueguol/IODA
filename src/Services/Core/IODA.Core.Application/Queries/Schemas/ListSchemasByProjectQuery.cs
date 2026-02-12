using IODA.Core.Application.DTOs;
using MediatR;

namespace IODA.Core.Application.Queries.Schemas;

public record ListSchemasByProjectQuery(
    Guid ProjectId,
    bool ActiveOnly = true) : IRequest<IReadOnlyList<ContentSchemaListItemDto>>;
