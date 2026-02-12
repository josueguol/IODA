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

        // Resolve inherited fields from parent chain
        List<FieldDefinitionDto>? inheritedFields = null;
        if (schema.ParentSchemaId.HasValue)
        {
            inheritedFields = new List<FieldDefinitionDto>();
            var visited = new HashSet<Guid> { schema.Id };
            var currentParentId = schema.ParentSchemaId;

            while (currentParentId.HasValue)
            {
                if (!visited.Add(currentParentId.Value))
                    break; // Circular reference protection

                var parent = await _schemaRepository.GetByIdAsync(currentParentId.Value, cancellationToken);
                if (parent is null) break;

                // Add parent fields that don't conflict with already collected fields
                var existingFieldNames = inheritedFields
                    .Select(f => f.FieldName)
                    .Concat(schema.Fields.Select(f => f.FieldName))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var field in parent.Fields.OrderBy(f => f.DisplayOrder))
                {
                    if (!existingFieldNames.Contains(field.FieldName))
                    {
                        inheritedFields.Add(field.ToDto());
                        existingFieldNames.Add(field.FieldName);
                    }
                }

                currentParentId = parent.ParentSchemaId;
            }
        }

        return schema.ToDto(inheritedFields);
    }
}
