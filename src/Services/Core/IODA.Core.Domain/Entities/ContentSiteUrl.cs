namespace IODA.Core.Domain.Entities;

/// <summary>
/// Ruta publicada de un contenido para un sitio específico.
/// El host/subpath base se toma del Site; aquí solo se guarda la ruta relativa (ej. "mi-slug" o "seccion/mi-slug").
/// </summary>
public class ContentSiteUrl
{
    public Guid ContentId { get; private set; }
    public Guid SiteId { get; private set; }
    public string Path { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public Content Content { get; private set; } = null!;
    public Site Site { get; private set; } = null!;

    private ContentSiteUrl() { }

    internal ContentSiteUrl(Guid contentId, Guid siteId, string path)
    {
        ContentId = contentId;
        SiteId = siteId;
        Path = NormalizePath(path);
        CreatedAt = DateTime.UtcNow;
    }

    public static ContentSiteUrl Create(Guid contentId, Guid siteId, string path)
    {
        return new ContentSiteUrl(contentId, siteId, path);
    }

    public void SetPath(string path)
    {
        Path = NormalizePath(path);
        UpdatedAt = DateTime.UtcNow;
    }

    public static string NormalizePath(string rawPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
            throw new ArgumentException("Path cannot be empty.", nameof(rawPath));

        var normalized = rawPath.Trim();
        normalized = normalized.Trim('/');
        normalized = normalized.ToLowerInvariant();

        while (normalized.Contains("//", StringComparison.Ordinal))
            normalized = normalized.Replace("//", "/", StringComparison.Ordinal);

        if (normalized.Length == 0)
            throw new ArgumentException("Path cannot be empty.", nameof(rawPath));

        // Segmentos permitidos: a-z, 0-9, guion y guion bajo.
        // Separador entre segmentos: "/"
        if (!System.Text.RegularExpressions.Regex.IsMatch(normalized, @"^[a-z0-9\-_]+(?:/[a-z0-9\-_]+)*$"))
            throw new ArgumentException("Path contains invalid characters.", nameof(rawPath));

        return normalized;
    }
}
