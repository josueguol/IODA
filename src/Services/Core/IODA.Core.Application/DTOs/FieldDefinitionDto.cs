using System;

namespace IODA.Core.Application.DTOs;

public record FieldDefinitionDto(
    Guid Id,
    [property: Obsolete("Use Slug as the technical key")]
    string FieldName,
    string Label,
    string Slug,
    string FieldType,
    bool IsRequired,
    object? DefaultValue,
    string? HelpText,
    Dictionary<string, object>? ValidationRules,
    int DisplayOrder);
