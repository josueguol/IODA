namespace IODA.Core.Application.DTOs;

/// <summary>Rule for a block type allowed in content using this schema (optional min/max occurrences).</summary>
public record AllowedBlockTypeRuleDto(string BlockType, int? MinOccurrences = null, int? MaxOccurrences = null);

public record ContentSchemaDto(
    Guid Id,
    string PublicId,
    Guid ProjectId,
    string SchemaName,
    string SchemaType,
    string? Description,
    Guid? ParentSchemaId,
    int SchemaVersion,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    Guid CreatedBy,
    IReadOnlyList<FieldDefinitionDto> Fields,
    IReadOnlyList<FieldDefinitionDto>? InheritedFields,
    IReadOnlyList<AllowedBlockTypeRuleDto> AllowedBlockTypes);

public record ContentSchemaListItemDto(
    Guid Id,
    string PublicId,
    string SchemaName,
    string SchemaType,
    Guid? ParentSchemaId,
    int SchemaVersion,
    bool IsActive,
    IReadOnlyList<AllowedBlockTypeRuleDto> AllowedBlockTypes);
