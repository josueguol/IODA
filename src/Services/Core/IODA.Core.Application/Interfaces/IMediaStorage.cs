namespace IODA.Core.Application.Interfaces;

/// <summary>
/// Almacenamiento de archivos de media (local, CDN, blob). Permite sustituir por implementación CDN más adelante.
/// </summary>
public interface IMediaStorage
{
    /// <summary>
    /// Guarda el archivo y devuelve la clave de almacenamiento (ruta relativa o identificador).
    /// </summary>
    Task<string> SaveAsync(Stream stream, string fileName, string contentType, Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Abre un stream de lectura del archivo por su clave.
    /// </summary>
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Indica si existe el archivo con la clave dada.
    /// </summary>
    Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina el archivo del almacenamiento (opcional; para borrado lógico no se usa).
    /// </summary>
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}
