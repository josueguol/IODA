namespace IODA.Core.Application.Commands.Schemas;

public record CreateSchemaFieldDto(
    string Label,
    string Slug,
    string FieldType,
    bool IsRequired = false,
    object? DefaultValue = null,
    string? HelpText = null,
    Dictionary<string, object>? ValidationRules = null,
    int DisplayOrder = 0);
