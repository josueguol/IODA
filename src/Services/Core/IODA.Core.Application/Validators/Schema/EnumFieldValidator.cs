using IODA.Core.Application.Interfaces;
using IODA.Core.Domain.Entities;

namespace IODA.Core.Application.Validators.Schema;

/// <summary>Valida campos de tipo enum contra allowedValues en ValidationRules.</summary>
public sealed class EnumFieldValidator : IFieldValidator
{
    public bool CanValidate(string fieldType) =>
        "enum".Equals(fieldType.Trim().ToLowerInvariant(), StringComparison.Ordinal);

    public IReadOnlyList<SchemaValidationError> Validate(FieldDefinition fieldDef, object? value)
    {
        if (value == null || (value is string sv && string.IsNullOrWhiteSpace(sv)))
            return Array.Empty<SchemaValidationError>();

        var rules = fieldDef.ValidationRules;
        if (rules == null || !rules.TryGetValue("allowedValues", out var allowedObj))
            return Array.Empty<SchemaValidationError>();

        var strValue = value is string st ? st : value.ToString();
        var allowed = SchemaValidationHelpers.GetAllowedValuesList(allowedObj);
        if (allowed.Count > 0 && !allowed.Contains(strValue ?? ""))
            return new[] { new SchemaValidationError(fieldDef.FieldName, $"Field '{fieldDef.FieldName}' must be one of: {string.Join(", ", allowed)}.") };

        return Array.Empty<SchemaValidationError>();
    }
}
