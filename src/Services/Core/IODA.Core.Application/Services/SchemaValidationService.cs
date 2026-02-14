using IODA.Core.Application.Interfaces;
using IODA.Core.Application.Validators.Schema;
using IODA.Core.Domain.Entities;

namespace IODA.Core.Application.Services;

/// <summary>
/// Validación centralizada de contenido contra esquema: tipos, requeridos y reglas reutilizables (minLength, maxLength, pattern, min, max).
/// Delega la validación por tipo a estrategias IFieldValidator (string, number, boolean, date, enum, etc.).
/// </summary>
public class SchemaValidationService : ISchemaValidationService
{
    private readonly IEnumerable<IFieldValidator> _fieldValidators;

    public SchemaValidationService(IEnumerable<IFieldValidator> fieldValidators)
    {
        _fieldValidators = fieldValidators?.ToList() ?? throw new ArgumentNullException(nameof(fieldValidators));
    }

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

            if (fieldDef.IsRequired && (value == null || IsEmptyValue(value)))
            {
                errors.Add(new SchemaValidationError(fieldDef.FieldName, $"Field '{fieldDef.FieldName}' is required."));
                continue;
            }

            if (value == null || (value is string s && string.IsNullOrWhiteSpace(s)))
            {
                if (!fieldDef.IsRequired)
                    continue;
            }

            var validator = _fieldValidators.FirstOrDefault(v => v.CanValidate(fieldDef.FieldType));
            if (validator != null)
            {
                var typeErrors = validator.Validate(fieldDef, value);
                errors.AddRange(typeErrors);
            }
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
}
