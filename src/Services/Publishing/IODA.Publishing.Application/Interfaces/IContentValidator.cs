namespace IODA.Publishing.Application.Interfaces;

/// <summary>
/// Valida contenido antes de aprobar publicación (ej. campos requeridos, esquema).
/// </summary>
public interface IContentValidator
{
    Task<ContentValidationResult> ValidateAsync(Guid projectId, Guid contentId, CancellationToken cancellationToken = default);
}

public record ContentValidationResult(bool IsValid, IReadOnlyList<string> Errors);
