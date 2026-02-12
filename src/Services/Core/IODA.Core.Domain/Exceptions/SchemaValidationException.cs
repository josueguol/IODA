using IODA.Shared.BuildingBlocks.Domain;

namespace IODA.Core.Domain.Exceptions;

/// <summary>
/// Se lanza cuando el contenido no cumple el esquema (campos requeridos, tipos, reglas).
/// Permite devolver 400 con lista de errores por campo (ProblemDetails).
/// </summary>
public class SchemaValidationException : DomainException
{
    /// <summary>
    /// Errores por campo: clave = nombre del campo, valor = mensaje de error.
    /// </summary>
    public IReadOnlyList<SchemaValidationErrorEntry> Errors { get; }

    public SchemaValidationException(IReadOnlyList<SchemaValidationErrorEntry> errors)
        : base("Content does not conform to the schema. See errors for details.")
    {
        Errors = errors;
    }
}

public record SchemaValidationErrorEntry(string Field, string Message);
