using System.Text.Json;
using System.Text.RegularExpressions;
using IODA.Core.Application.Interfaces;
using IODA.Core.Domain.Entities;

namespace IODA.Core.Application.Validators.Schema;

/// <summary>Valida campos de tipo string, text y richtext (minLength, maxLength, pattern).</summary>
public sealed class StringFieldValidator : IFieldValidator
{
    private static readonly string[] Types = { "string", "text", "richtext" };

    public bool CanValidate(string fieldType) =>
        Types.Contains(fieldType.Trim().ToLowerInvariant());

    public IReadOnlyList<SchemaValidationError> Validate(FieldDefinition fieldDef, object? value)
    {
        var errors = new List<SchemaValidationError>();
        string? str = null;
        if (value != null)
        {
            if (value is string s)
                str = s;
            else if (value is JsonElement je)
                str = je.GetString();
            else
                str = value.ToString();
        }

        if (str == null && fieldDef.IsRequired)
        {
            errors.Add(new SchemaValidationError(fieldDef.FieldName, $"Field '{fieldDef.FieldName}' must be a string."));
            return errors;
        }

        if (str == null)
            return errors;

        var rules = fieldDef.ValidationRules;
        if (rules == null)
            return errors;

        if (rules.TryGetValue("minLength", out var minLenObj) && SchemaValidationHelpers.TryGetInt(minLenObj, out var minLen) && str.Length < minLen)
            errors.Add(new SchemaValidationError(fieldDef.FieldName, $"Field '{fieldDef.FieldName}' must be at least {minLen} characters."));

        if (rules.TryGetValue("maxLength", out var maxLenObj) && SchemaValidationHelpers.TryGetInt(maxLenObj, out var maxLen) && str.Length > maxLen)
            errors.Add(new SchemaValidationError(fieldDef.FieldName, $"Field '{fieldDef.FieldName}' must not exceed {maxLen} characters."));

        if (rules.TryGetValue("pattern", out var patternObj) && patternObj != null)
        {
            var pattern = patternObj is string p ? p : patternObj.ToString();
            if (!string.IsNullOrEmpty(pattern) && !Regex.IsMatch(str, pattern))
                errors.Add(new SchemaValidationError(fieldDef.FieldName, $"Field '{fieldDef.FieldName}' does not match the required format."));
        }

        return errors;
    }
}
