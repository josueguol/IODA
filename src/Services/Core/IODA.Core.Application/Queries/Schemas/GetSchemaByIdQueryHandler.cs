using IODA.Core.Application.DTOs;
using IODA.Core.Application.Mappings;
using IODA.Core.Domain.Repositories;
using MediatR;

namespace IODA.Core.Application.Queries.Schemas;

public class GetSchemaByIdQueryHandler : IRequestHandler<GetSchemaByIdQuery, ContentSchemaDto?>
{
    private readonly IContentSchemaRepository _schemaRepository;

    public GetSchemaByIdQueryHandler(IContentSchemaRepository schemaRepository)
    {
        _schemaRepository = schemaRepository;
    }

    public async Task<ContentSchemaDto?> Handle(GetSchemaByIdQuery request, CancellationToken cancellationToken)
    {
        var schema = await _schemaRepository.GetByIdAsync(request.SchemaId, cancellationToken);
        if (schema is null) return null;
        return schema.ToDto(null);
    }
}
