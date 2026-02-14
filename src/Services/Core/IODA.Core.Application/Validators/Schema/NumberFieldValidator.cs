using System.Text.Json;
using IODA.Core.Application.Interfaces;
using IODA.Core.Domain.Entities;

namespace IODA.Core.Application.Validators.Schema;

/// <summary>Valida campos de tipo number e integer (min, max).</summary>
public sealed class NumberFieldValidator : IFieldValidator
{
    private static readonly string[] Types = { "number", "integer" };

    public bool CanValidate(string fieldType) =>
        Types.Contains(fieldType.Trim().ToLowerInvariant());

    public IReadOnlyList<SchemaValidationError> Validate(FieldDefinition fieldDef, object? value)
    {
        var errors = new List<SchemaValidationError>();
        if (value == null || (value is string sv && string.IsNullOrWhiteSpace(sv)))
            return errors;

        if (!TryGetDecimal(value, out var num))
        {
            errors.Add(new SchemaValidationError(fieldDef.FieldName, $"Field '{fieldDef.FieldName}' must be a number."));
            return errors;
        }

        var rules = fieldDef.ValidationRules;
        if (rules == null)
            return errors;

        if (rules.TryGetValue("min", out var minObj) && SchemaValidationHelpers.TryGetDecimal(minObj, out var min) && num < min)
            errors.Add(new SchemaValidationError(fieldDef.FieldName, $"Field '{fieldDef.FieldName}' must be at least {min}."));

        if (rules.TryGetValue("max", out var maxObj) && SchemaValidationHelpers.TryGetDecimal(maxObj, out var max) && num > max)
            errors.Add(new SchemaValidationError(fieldDef.FieldName, $"Field '{fieldDef.FieldName}' must not exceed {max}."));

        return errors;
    }

    private static bool TryGetDecimal(object value, out decimal result)
    {
        result = 0;
        if (value is decimal d)
        {
            result = d;
            return true;
        }

        if (value is double db)
        {
            result = (decimal)db;
            return true;
        }

        if (value is int i)
        {
            result = i;
            return true;
        }

        if (value is long l)
        {
            result = l;
            return true;
        }

        if (value is JsonElement je && je.TryGetDecimal(out var j))
        {
            result = j;
            return true;
        }

        if (value is string str && decimal.TryParse(str, System.Globalization.NumberStyles.Any, null, out var p))
        {
            result = p;
            return true;
        }

        return false;
    }
}
