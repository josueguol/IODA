using System.Text.Json;
using IODA.Core.Application.Interfaces;
using IODA.Core.Domain.Entities;

namespace IODA.Core.Application.Validators.Schema;

/// <summary>Valida campos de tipo date y datetime.</summary>
public sealed class DateFieldValidator : IFieldValidator
{
    private static readonly string[] Types = { "date", "datetime" };

    public bool CanValidate(string fieldType) =>
        Types.Contains(fieldType.Trim().ToLowerInvariant());

    public IReadOnlyList<SchemaValidationError> Validate(FieldDefinition fieldDef, object? value)
    {
        if (value == null || (value is string sv && string.IsNullOrWhiteSpace(sv)))
            return Array.Empty<SchemaValidationError>();

        if (value is DateTime or DateTimeOffset)
            return Array.Empty<SchemaValidationError>();
        if (value is string str && DateTime.TryParse(str, out _))
            return Array.Empty<SchemaValidationError>();
        if (value is JsonElement je && je.ValueKind == JsonValueKind.String && DateTime.TryParse(je.GetString(), out _))
            return Array.Empty<SchemaValidationError>();

        return new[] { new SchemaValidationError(fieldDef.FieldName, $"Field '{fieldDef.FieldName}' must be a valid date or datetime.") };
    }
}
