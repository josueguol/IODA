using IODA.Core.Application.DTOs;
using IODA.Core.Application.Schemas;
using IODA.Core.Domain.Entities;

namespace IODA.Core.Application.Mappings;

public static class SchemaMappings
{
    public static ContentSchemaDto ToDto(this ContentSchema schema, IReadOnlyList<FieldDefinitionDto>? inheritedFields = null)
    {
        return new ContentSchemaDto(
            schema.Id,
            schema.PublicId.FullId,
            schema.ProjectId,
            schema.SchemaName,
            schema.SchemaType,
            schema.Description,
            null,
            schema.SchemaVersion,
            schema.IsActive,
            schema.CreatedAt,
            schema.UpdatedAt,
            schema.CreatedBy,
            schema.Fields.Select(f => f.ToDto()).ToList(),
            inheritedFields,
            schema.AllowedBlockTypes.Select(r => new AllowedBlockTypeRuleDto(r.BlockType, r.MinOccurrences, r.MaxOccurrences)).ToList());
    }

    public static ContentSchemaListItemDto ToListItemDto(this ContentSchema schema)
    {
        return new ContentSchemaListItemDto(
            schema.Id,
            schema.PublicId.FullId,
            schema.SchemaName,
            schema.SchemaType,
            null,
            schema.SchemaVersion,
            schema.IsActive,
            schema.AllowedBlockTypes.Select(r => new AllowedBlockTypeRuleDto(r.BlockType, r.MinOccurrences, r.MaxOccurrences)).ToList());
    }

    public static FieldDefinitionDto ToDto(this FieldDefinition field)
    {
        return new FieldDefinitionDto(
            field.Id,
            field.Slug, // FieldName is obsolete; use Slug for backward compatibility
            field.Label,
            field.Slug,
            FieldTypeCanonicalizer.Canonicalize(field.FieldType),
            field.IsRequired,
            field.DefaultValue,
            field.HelpText,
            field.ValidationRules,
            field.DisplayOrder);
    }
}
