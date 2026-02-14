using System.Text.Json;
using IODA.Core.Application.Interfaces;
using IODA.Core.Domain.Entities;

namespace IODA.Core.Application.Validators.Schema;

/// <summary>Valida campos de tipo boolean.</summary>
public sealed class BooleanFieldValidator : IFieldValidator
{
    public bool CanValidate(string fieldType) =>
        "boolean".Equals(fieldType.Trim().ToLowerInvariant(), StringComparison.Ordinal);

    public IReadOnlyList<SchemaValidationError> Validate(FieldDefinition fieldDef, object? value)
    {
        if (value == null || (value is string s && string.IsNullOrWhiteSpace(s)))
            return Array.Empty<SchemaValidationError>();

        if (value is bool)
            return Array.Empty<SchemaValidationError>();
        if (value is string str && (str.Equals("true", StringComparison.OrdinalIgnoreCase) || str.Equals("false", StringComparison.OrdinalIgnoreCase)))
            return Array.Empty<SchemaValidationError>();
        if (value is JsonElement je && (je.ValueKind == JsonValueKind.True || je.ValueKind == JsonValueKind.False))
            return Array.Empty<SchemaValidationError>();

        return new[] { new SchemaValidationError(fieldDef.FieldName, $"Field '{fieldDef.FieldName}' must be a boolean.") };
    }
}
