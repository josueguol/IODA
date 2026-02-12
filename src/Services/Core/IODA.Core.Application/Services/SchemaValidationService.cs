using System.Text.Json;
using System.Text.RegularExpressions;
using IODA.Core.Application.Interfaces;
using IODA.Core.Domain.Entities;

namespace IODA.Core.Application.Services;

/// <summary>
/// Validación centralizada de contenido contra esquema: tipos, requeridos y reglas reutilizables (minLength, maxLength, pattern, min, max).
/// Compatible con la evolución del esquema: solo se validan los campos definidos en el esquema; campos extra se ignoran.
/// </summary>
public class SchemaValidationService : ISchemaValidationService
{
    public SchemaValidationResult Validate(ContentSchema schema, IReadOnlyDictionary<string, object> fields)
    {
        if (schema == null)
            throw new ArgumentNullException(nameof(schema));
        fields ??= new Dictionary<string, object>();

        var errors = new List<SchemaValidationError>();
        var fieldsByName = schema.Fields.OrderBy(f => f.DisplayOrder).ToList();

        foreach (var fieldDef in fieldsByName)
        {
            var value = fields.TryGetValue(fieldDef.FieldName, out var v) ? v : null;

            // Required
            if (fieldDef.IsRequired && (value == null || IsEmptyValue(value)))
            {
                errors.Add(new SchemaValidationError(fieldDef.FieldName, $"Field '{fieldDef.FieldName}' is required."));
                continue;
            }

            // Si no está presente y no es requerido, aplicar default no hace falta validar tipo
            if (value == null || (value is string s && string.IsNullOrWhiteSpace(s)))
            {
                if (!fieldDef.IsRequired)
                    continue;
            }

            var typeErrors = ValidateFieldTypeAndRules(fieldDef, value);
            errors.AddRange(typeErrors);
        }

        return new SchemaValidationResult(errors.Count == 0, errors);
    }

    private static bool IsEmptyValue(object value)
    {
        return value switch
        {
            null => true,
            string str => string.IsNullOrWhiteSpace(str),
            _ => false
        };
    }

    private static List<SchemaValidationError> ValidateFieldTypeAndRules(FieldDefinition fieldDef, object? value)
    {
        var errors = new List<SchemaValidationError>();
        var typeLower = fieldDef.FieldType.Trim().ToLowerInvariant();

        switch (typeLower)
        {
            case "string":
            case "text":
            case "richtext":
                ValidateString(fieldDef, value, errors);
                break;
            case "number":
            case "integer":
                ValidateNumber(fieldDef, value, errors);
                break;
            case "boolean":
                ValidateBoolean(fieldDef, value, errors);
                break;
            case "date":
            case "datetime":
                ValidateDate(fieldDef, value, errors);
                break;
            case "enum":
                ValidateEnum(fieldDef, value, errors);
                break;
            case "list":
                // Acepta arrays (lista de valores); sin reglas estrictas por defecto
                break;
            case "media":
                // Acepta string (media ID) o null; sin reglas estrictas por defecto
                break;
            case "json":
            case "reference":
                // Aceptamos string o objeto; sin reglas estrictas por defecto
                break;
            default:
                // Tipo desconocido: tratar como string solo si el valor no es un array
                if (value is not List<object> && !(value is JsonElement aje && aje.ValueKind == JsonValueKind.Array))
                    ValidateString(fieldDef, value, errors);
                break;
        }

        return errors;
    }

    private static void ValidateString(FieldDefinition fieldDef, object? value, List<SchemaValidationError> errors)
    {
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
            return;
        }

        if (str == null)
            return;

        var rules = fieldDef.ValidationRules;
        if (rules == null)
            return;

        if (rules.TryGetValue("minLength", out var minLenObj) && TryGetInt(minLenObj, out var minLen) && str.Length < minLen)
            errors.Add(new SchemaValidationError(fieldDef.FieldName, $"Field '{fieldDef.FieldName}' must be at least {minLen} characters."));

        if (rules.TryGetValue("maxLength", out var maxLenObj) && TryGetInt(maxLenObj, out var maxLen) && str.Length > maxLen)
            errors.Add(new SchemaValidationError(fieldDef.FieldName, $"Field '{fieldDef.FieldName}' must not exceed {maxLen} characters."));

        if (rules.TryGetValue("pattern", out var patternObj) && patternObj != null)
        {
            var pattern = patternObj is string p ? p : patternObj.ToString();
            if (!string.IsNullOrEmpty(pattern) && !Regex.IsMatch(str, pattern))
                errors.Add(new SchemaValidationError(fieldDef.FieldName, $"Field '{fieldDef.FieldName}' does not match the required format."));
        }
    }

    private static void ValidateNumber(FieldDefinition fieldDef, object? value, List<SchemaValidationError> errors)
    {
        if (value == null || (value is string sv && string.IsNullOrWhiteSpace(sv)))
            return;

        if (!TryGetDecimal(value, out var num))
        {
            errors.Add(new SchemaValidationError(fieldDef.FieldName, $"Field '{fieldDef.FieldName}' must be a number."));
            return;
        }

        var rules = fieldDef.ValidationRules;
        if (rules == null)
            return;

        if (rules.TryGetValue("min", out var minObj) && TryGetDecimal(minObj, out var min) && num < min)
            errors.Add(new SchemaValidationError(fieldDef.FieldName, $"Field '{fieldDef.FieldName}' must be at least {min}."));

        if (rules.TryGetValue("max", out var maxObj) && TryGetDecimal(maxObj, out var max) && num > max)
            errors.Add(new SchemaValidationError(fieldDef.FieldName, $"Field '{fieldDef.FieldName}' must not exceed {max}."));
    }

    private static void ValidateBoolean(FieldDefinition fieldDef, object? value, List<SchemaValidationError> errors)
    {
        if (value == null || (value is string s && string.IsNullOrWhiteSpace(s)))
            return;

        if (value is bool)
            return;
        if (value is string str && (str.Equals("true", StringComparison.OrdinalIgnoreCase) || str.Equals("false", StringComparison.OrdinalIgnoreCase)))
            return;
        if (value is JsonElement je && (je.ValueKind == JsonValueKind.True || je.ValueKind == JsonValueKind.False))
            return;

        errors.Add(new SchemaValidationError(fieldDef.FieldName, $"Field '{fieldDef.FieldName}' must be a boolean."));
    }

    private static void ValidateDate(FieldDefinition fieldDef, object? value, List<SchemaValidationError> errors)
    {
        if (value == null || (value is string sv && string.IsNullOrWhiteSpace(sv)))
            return;

        if (value is DateTime or DateTimeOffset)
            return;
        if (value is string str && DateTime.TryParse(str, out _))
            return;
        if (value is JsonElement je && je.ValueKind == JsonValueKind.String && DateTime.TryParse(je.GetString(), out _))
            return;

        errors.Add(new SchemaValidationError(fieldDef.FieldName, $"Field '{fieldDef.FieldName}' must be a valid date or datetime."));
    }

    private static void ValidateEnum(FieldDefinition fieldDef, object? value, List<SchemaValidationError> errors)
    {
        if (value == null || (value is string sv && string.IsNullOrWhiteSpace(sv)))
            return;

        // Si el esquema define "allowedValues" en ValidationRules, validar
        var rules = fieldDef.ValidationRules;
        if (rules != null && rules.TryGetValue("allowedValues", out var allowedObj))
        {
            var strValue = value is string st ? st : value.ToString();
            var allowed = GetAllowedValuesList(allowedObj);
            if (allowed.Count > 0 && !allowed.Contains(strValue ?? ""))
                errors.Add(new SchemaValidationError(fieldDef.FieldName, $"Field '{fieldDef.FieldName}' must be one of: {string.Join(", ", allowed)}."));
        }
    }

    private static bool TryGetInt(object? obj, out int result)
    {
        result = 0;
        if (obj == null) return false;
        if (obj is int i) { result = i; return true; }
        if (obj is long l) { result = (int)l; return true; }
        if (obj is JsonElement je && je.TryGetInt32(out var j)) { result = j; return true; }
        if (obj is string str && int.TryParse(str, out var p)) { result = p; return true; }
        return false;
    }

    private static List<string> GetAllowedValuesList(object? allowedObj)
    {
        var allowed = new List<string>();
        if (allowedObj is JsonElement arrEl && arrEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var e in arrEl.EnumerateArray())
                allowed.Add(e.GetString() ?? "");
        }
        else if (allowedObj is IList<object> list)
        {
            foreach (var o in list)
                allowed.Add(o?.ToString() ?? "");
        }
        return allowed;
    }

    private static bool TryGetDecimal(object? obj, out decimal result)
    {
        result = 0;
        if (obj == null) return false;
        if (obj is decimal d) { result = d; return true; }
        if (obj is double db) { result = (decimal)db; return true; }
        if (obj is int i) { result = i; return true; }
        if (obj is long l) { result = l; return true; }
        if (obj is JsonElement je && je.TryGetDecimal(out var j)) { result = j; return true; }
        if (obj is string str && decimal.TryParse(str, System.Globalization.NumberStyles.Any, null, out var p)) { result = p; return true; }
        return false;
    }
}
