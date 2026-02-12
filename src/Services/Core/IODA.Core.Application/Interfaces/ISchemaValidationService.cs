using IODA.Core.Domain.Entities;

namespace IODA.Core.Application.Interfaces;

/// <summary>
/// Valida el diccionario de campos de un contenido contra el esquema (tipos, requeridos, reglas reutilizables).
/// Centraliza la lógica de validación para Create/Update de contenido y evolución del esquema sin romper contenido existente.
/// </summary>
public interface ISchemaValidationService
{
    /// <summary>
    /// Valida que <paramref name="fields"/> cumpla con las definiciones del <paramref name="schema"/>.
    /// Solo se validan campos definidos en el esquema; se permiten campos adicionales (no se rechazan) para evolución.
    /// </summary>
    /// <param name="schema">Esquema del tipo de contenido (con FieldDefinitions).</param>
    /// <param name="fields">Campos del contenido (payload de create/update).</param>
    /// <returns>Resultado con IsValid y lista de errores por campo.</returns>
    SchemaValidationResult Validate(ContentSchema schema, IReadOnlyDictionary<string, object> fields);
}

/// <summary>
/// Resultado de la validación de contenido contra esquema.
/// </summary>
/// <param name="IsValid">True si no hay errores.</param>
/// <param name="Errors">Errores por campo (Field = nombre del campo, Message = mensaje de error).</param>
public record SchemaValidationResult(bool IsValid, IReadOnlyList<SchemaValidationError> Errors);

public record SchemaValidationError(string Field, string Message);
