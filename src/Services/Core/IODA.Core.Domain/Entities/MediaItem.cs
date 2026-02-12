using IODA.Core.Domain.ValueObjects;
using IODA.Shared.BuildingBlocks.Domain;

namespace IODA.Core.Domain.Entities;

/// <summary>
/// Representa un archivo de media subido (imagen, documento, etc.) asociado a un proyecto.
/// Incluye metadatos y versionado (Version = 1 por defecto; ampliable con MediaItemVersion).
/// </summary>
public class MediaItem : AggregateRoot<Guid>
{
    public Identifier PublicId { get; private set; } = null!;
    public Guid ProjectId { get; private set; }
    public string FileName { get; private set; } = null!;
    public string DisplayName { get; private set; } = null!;
    public string ContentType { get; private set; } = null!;
    public long SizeBytes { get; private set; }
    /// <summary>Ruta relativa o clave de almacenamiento (ej. projectId/guid_filename).</summary>
    public string StorageKey { get; private set; } = null!;
    public int Version { get; private set; }
    /// <summary>Metadatos adicionales (alt text, caption, etc.) en JSON.</summary>
    public Dictionary<string, object>? Metadata { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }

    public Project Project { get; private set; } = null!;

    private MediaItem() { }

    private MediaItem(
        Guid id,
        Identifier publicId,
        Guid projectId,
        string fileName,
        string displayName,
        string contentType,
        long sizeBytes,
        string storageKey,
        int version,
        Dictionary<string, object>? metadata,
        Guid createdBy)
    {
        Id = id;
        PublicId = publicId;
        ProjectId = projectId;
        FileName = fileName;
        DisplayName = displayName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        StorageKey = storageKey;
        Version = version;
        Metadata = metadata;
        CreatedAt = DateTime.UtcNow;
        CreatedBy = createdBy;
    }

    public static MediaItem Create(
        Guid projectId,
        string fileName,
        string displayName,
        string contentType,
        long sizeBytes,
        string storageKey,
        Guid createdBy,
        Dictionary<string, object>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be empty", nameof(fileName));
        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("Content type cannot be empty", nameof(contentType));
        if (string.IsNullOrWhiteSpace(storageKey))
            throw new ArgumentException("Storage key cannot be empty", nameof(storageKey));
        if (sizeBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Size must be non-negative.");

        var id = Guid.NewGuid();
        var publicId = Identifier.Create("med");
        var name = string.IsNullOrWhiteSpace(displayName) ? fileName : displayName;

        return new MediaItem(
            id,
            publicId,
            projectId,
            fileName,
            name,
            contentType,
            sizeBytes,
            storageKey,
            version: 1,
            metadata,
            createdBy);
    }

    public void UpdateMetadata(Dictionary<string, object>? metadata)
    {
        Metadata = metadata;
    }

    public void UpdateDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name cannot be empty", nameof(displayName));
        DisplayName = displayName;
    }
}
