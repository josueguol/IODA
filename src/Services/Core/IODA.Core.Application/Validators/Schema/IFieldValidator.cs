using IODA.Core.Application.Interfaces;
using IODA.Core.Domain.Entities;

namespace IODA.Core.Application.Validators.Schema;

/// <summary>
/// Valida un valor contra la definición de un campo del esquema (tipo y reglas).
/// Cada implementación cubre uno o más tipos de campo (string, number, boolean, date, enum, etc.).
/// </summary>
public interface IFieldValidator
{
    /// <summary>
    /// Indica si este validador puede validar el tipo de campo dado (ej. "string", "number").
    /// </summary>
    bool CanValidate(string fieldType);

    /// <summary>
    /// Valida el valor para el campo definido. Devuelve una lista de errores (vacía si es válido).
    /// No comprueba "required"; eso lo hace el orquestador.
    /// </summary>
    IReadOnlyList<SchemaValidationError> Validate(FieldDefinition fieldDef, object? value);
}
