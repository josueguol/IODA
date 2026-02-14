using System.Text.Json;
using IODA.Core.Application.Interfaces;
using IODA.Core.Domain.Entities;

namespace IODA.Core.Application.Validators.Schema;

/// <summary>
/// Fallback: para tipos list, media, json, reference no aplicamos reglas; para tipo desconocido, tratamos como string si el valor no es array.
/// Debe registrarse después de los validadores específicos (string, number, boolean, date, enum).
/// </summary>
public sealed class DefaultFieldValidator : IFieldValidator
{
    private static readonly string[] PassthroughTypes = { "list", "media", "json", "reference" };
    private readonly StringFieldValidator _stringValidator = new();

    /// <summary>Siempre true para actuar como fallback; el orquestador lo usa en último lugar.</summary>
    public bool CanValidate(string _) => true;

    public IReadOnlyList<SchemaValidationError> Validate(FieldDefinition fieldDef, object? value)
    {
        var lower = fieldDef.FieldType.Trim().ToLowerInvariant();
        if (PassthroughTypes.Contains(lower))
            return Array.Empty<SchemaValidationError>();

        // Tipo desconocido: validar como string solo si no es array
        if (value is List<object> || (value is JsonElement aje && aje.ValueKind == JsonValueKind.Array))
            return Array.Empty<SchemaValidationError>();

        return _stringValidator.Validate(fieldDef, value);
    }
}
