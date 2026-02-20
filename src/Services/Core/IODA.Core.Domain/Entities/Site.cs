using IODA.Core.Domain.ValueObjects;
using IODA.Shared.BuildingBlocks.Domain;

namespace IODA.Core.Domain.Entities;

/// <summary>
/// Representa un sitio web dentro de un proyecto. Un sitio puede estar asociado a un entorno específico
/// o ser global al proyecto. Define dominio, subdominio, subruta y tema asociado.
/// </summary>
public class Site : AggregateRoot<Guid>
{
    public Identifier PublicId { get; private set; } = null!;
    public Guid ProjectId { get; private set; }
    public Guid? EnvironmentId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Domain { get; private set; } = null!;
    public string? Subdomain { get; private set; }
    public string? Subpath { get; private set; }
    public string? ThemeId { get; private set; }
    /// <summary>Plantilla de URL para resolución de rutas (Req 5). Placeholders: {slug}, {createdAt:format}, {section}, campos custom. Ej: "/{slug}" o "/{section}/{createdAt:yyyy/MM}/{slug}".</summary>
    public string? UrlTemplate { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }

    // Navigation
    public Project Project { get; private set; } = null!;
    public Environment? Environment { get; private set; }

    private Site() { }

    private Site(
        Guid id,
        Identifier publicId,
        Guid projectId,
        Guid? environmentId,
        string name,
        string domain,
        string? subdomain,
        string? subpath,
        string? themeId,
        string? urlTemplate,
        Guid createdBy)
    {
        Id = id;
        PublicId = publicId;
        ProjectId = projectId;
        EnvironmentId = environmentId;
        Name = name;
        Domain = domain;
        Subdomain = subdomain;
        Subpath = subpath;
        ThemeId = themeId;
        UrlTemplate = urlTemplate;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        CreatedBy = createdBy;
    }

    public static Site Create(
        Guid projectId,
        Guid? environmentId,
        string name,
        string domain,
        string? subdomain = null,
        string? subpath = null,
        string? themeId = null,
        string? urlTemplate = null,
        Guid createdBy = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Site name cannot be empty", nameof(name));
        if (string.IsNullOrWhiteSpace(domain))
            throw new ArgumentException("Domain cannot be empty", nameof(domain));

        var id = Guid.NewGuid();
        var publicId = Identifier.Create("sit");

        return new Site(id, publicId, projectId, environmentId, name, domain, subdomain, subpath, themeId, urlTemplate, createdBy);
    }

    public void Update(
        string name,
        string domain,
        string? subdomain = null,
        string? subpath = null,
        string? themeId = null,
        string? urlTemplate = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Site name cannot be empty", nameof(name));
        if (string.IsNullOrWhiteSpace(domain))
            throw new ArgumentException("Domain cannot be empty", nameof(domain));

        Name = name;
        Domain = domain;
        Subdomain = subdomain;
        Subpath = subpath;
        ThemeId = themeId;
        UrlTemplate = string.IsNullOrWhiteSpace(urlTemplate) ? null : urlTemplate.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeEnvironment(Guid? environmentId)
    {
        EnvironmentId = environmentId;
        UpdatedAt = DateTime.UtcNow;
    }
}
