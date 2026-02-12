using IODA.Core.Application.DTOs;
using MediatR;

namespace IODA.Core.Application.Queries.Schemas;

public record GetSchemaByIdQuery(Guid SchemaId) : IRequest<ContentSchemaDto?>;
