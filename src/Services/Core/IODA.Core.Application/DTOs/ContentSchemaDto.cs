namespace IODA.Core.Application.DTOs;

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
    IReadOnlyList<FieldDefinitionDto>? InheritedFields);

public record ContentSchemaListItemDto(
    Guid Id,
    string PublicId,
    string SchemaName,
    string SchemaType,
    Guid? ParentSchemaId,
    int SchemaVersion,
    bool IsActive);
