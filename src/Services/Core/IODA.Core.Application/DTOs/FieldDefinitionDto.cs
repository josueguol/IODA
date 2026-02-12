namespace IODA.Core.Application.DTOs;

public record FieldDefinitionDto(
    Guid Id,
    string FieldName,
    string FieldType,
    bool IsRequired,
    object? DefaultValue,
    string? HelpText,
    Dictionary<string, object>? ValidationRules,
    int DisplayOrder);
