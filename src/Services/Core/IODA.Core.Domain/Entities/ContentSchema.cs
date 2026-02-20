using IODA.Core.Domain.ValueObjects;
using IODA.Shared.BuildingBlocks.Domain;

namespace IODA.Core.Domain.Entities;

/// <summary>
/// Defines the schema/structure for a content type
/// This is what makes the CMS "schema-driven"
/// </summary>
public class ContentSchema : AggregateRoot<Guid>
{
    private readonly List<FieldDefinition> _fields = [];

    public Identifier PublicId { get; private set; } = null!;
    public Guid ProjectId { get; private set; }
    public string SchemaName { get; private set; } = null!;
    public string SchemaType { get; private set; } = null!;
    public string? Description { get; private set; }
    /// <summary>
    /// Optional parent schema for inheritance. Child schema inherits all parent fields.
    /// </summary>
    public Guid? ParentSchemaId { get; private set; }
    public int SchemaVersion { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }

    // Navigation
    public Project Project { get; private set; } = null!;
    public ContentSchema? ParentSchema { get; private set; }
    public IReadOnlyCollection<FieldDefinition> Fields => _fields.AsReadOnly();

    // EF Core constructor
    private ContentSchema() { }

    private ContentSchema(
        Guid id,
        Identifier publicId,
        Guid projectId,
        string schemaName,
        string schemaType,
        string? description,
        Guid? parentSchemaId,
        List<FieldDefinition> fields,
        Guid createdBy)
    {
        Id = id;
        PublicId = publicId;
        ProjectId = projectId;
        SchemaName = schemaName;
        SchemaType = schemaType;
        Description = description;
        ParentSchemaId = parentSchemaId;
        SchemaVersion = 1;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        CreatedBy = createdBy;
        _fields = fields;

        // Assign schema id to all fields (when created from handler, they use Guid.Empty)
        foreach (var field in _fields)
        {
            field.SetSchemaId(id);
        }

        RaiseDomainEvent(new SchemaCreatedDomainEvent(Id, schemaName, schemaType));
    }

    public static ContentSchema Create(
        Guid projectId,
        string schemaName,
        string schemaType,
        string? description,
        List<FieldDefinition> fields,
        Guid createdBy,
        Guid? parentSchemaId = null)
    {
        if (string.IsNullOrWhiteSpace(schemaName))
        {
            throw new ArgumentException("Schema name cannot be empty", nameof(schemaName));
        }

        if (string.IsNullOrWhiteSpace(schemaType))
        {
            throw new ArgumentException("Schema type cannot be empty", nameof(schemaType));
        }

        if (fields == null || fields.Count == 0)
        {
            throw new ArgumentException("Schema must have at least one field", nameof(fields));
        }

        var id = Guid.NewGuid();
        var publicId = Identifier.Create("sch");

        return new ContentSchema(
            id,
            publicId,
            projectId,
            schemaName,
            schemaType,
            description,
            parentSchemaId,
            fields,
            createdBy);
    }

    public void AddField(FieldDefinition field)
    {
        if (_fields.Any(f => f.Slug == field.Slug))
        {
            throw new InvalidOperationException($"A field with slug '{field.Slug}' already exists in this schema.");
        }

        _fields.Add(field);
        SchemaVersion++;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new SchemaUpdatedDomainEvent(Id, SchemaName, SchemaVersion));
    }

    public void RemoveField(string slug)
    {
        var field = _fields.FirstOrDefault(f => f.Slug == slug);
        if (field == null)
        {
            throw new InvalidOperationException($"Field with slug '{slug}' not found.");
        }

        _fields.Remove(field);
        SchemaVersion++;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new SchemaUpdatedDomainEvent(Id, SchemaName, SchemaVersion));
    }

    public void UpdateField(string slug, FieldDefinition updatedField)
    {
        var field = _fields.FirstOrDefault(f => f.Slug == slug);
        if (field == null)
        {
            throw new InvalidOperationException($"Field with slug '{slug}' not found.");
        }

        if (updatedField.Slug != slug && _fields.Any(f => f.Slug == updatedField.Slug))
        {
            throw new InvalidOperationException($"A field with slug '{updatedField.Slug}' already exists in this schema.");
        }

        _fields.Remove(field);
        _fields.Add(updatedField);
        SchemaVersion++;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new SchemaUpdatedDomainEvent(Id, SchemaName, SchemaVersion));
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Defines a field within a schema. Label is visible in UI; Slug is the technical key (kebab-case, unique per schema).
/// FieldName is kept for backward compatibility and equals Slug for new fields (used as key in content JSON).
/// </summary>
public class FieldDefinition : Entity<Guid>
{
    public Guid SchemaId { get; private set; }
    /// <summary>Technical key for APIs and content storage; kebab-case, unique per schema. Same as Slug for new fields.</summary>
    public string FieldName { get; private set; } = null!;
    /// <summary>Human-readable label for UI (e.g. "Descripción corta").</summary>
    public string Label { get; private set; } = null!;
    /// <summary>Technical slug: kebab-case, unique within schema (e.g. "descripcion-corta").</summary>
    public string Slug { get; private set; } = null!;
    public string FieldType { get; private set; } = null!;
    public bool IsRequired { get; private set; }
    public object? DefaultValue { get; private set; }
    public string? HelpText { get; private set; }
    public Dictionary<string, object>? ValidationRules { get; private set; }
    public int DisplayOrder { get; private set; }

    // Navigation
    public ContentSchema Schema { get; private set; } = null!;

    private FieldDefinition() { }

    private FieldDefinition(
        Guid id,
        Guid schemaId,
        string fieldName,
        string label,
        string slug,
        string fieldType,
        bool isRequired,
        object? defaultValue,
        string? helpText,
        Dictionary<string, object>? validationRules,
        int displayOrder)
    {
        Id = id;
        SchemaId = schemaId;
        FieldName = fieldName;
        Label = label;
        Slug = slug;
        FieldType = fieldType;
        IsRequired = isRequired;
        DefaultValue = defaultValue;
        HelpText = helpText;
        ValidationRules = validationRules;
        DisplayOrder = displayOrder;
    }

    internal void SetSchemaId(Guid schemaId)
    {
        SchemaId = schemaId;
    }

    /// <summary>Kebab-case: lowercase, hyphens, no spaces. Regex: ^[a-z0-9]+(-[a-z0-9]+)*$</summary>
    public static bool IsValidSlugFormat(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return false;
        return System.Text.RegularExpressions.Regex.IsMatch(slug, @"^[a-z0-9]+(-[a-z0-9]+)*$");
    }

    /// <summary>Convert label to slug (kebab-case). Non-alphanumeric to hyphen; collapse hyphens; lowercase.</summary>
    public static string LabelToSlug(string label)
    {
        if (string.IsNullOrWhiteSpace(label)) return string.Empty;
        var normalized = System.Text.RegularExpressions.Regex.Replace(label.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "-");
        return System.Text.RegularExpressions.Regex.Replace(normalized, @"-+", "-").Trim('-');
    }

    public static FieldDefinition Create(
        Guid schemaId,
        string label,
        string slug,
        string fieldType,
        bool isRequired = false,
        object? defaultValue = null,
        string? helpText = null,
        Dictionary<string, object>? validationRules = null,
        int displayOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(label))
            throw new ArgumentException("Field label cannot be empty", nameof(label));
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Field slug cannot be empty", nameof(slug));
        if (!IsValidSlugFormat(slug))
            throw new ArgumentException("Slug must be kebab-case (lowercase letters, numbers, hyphens only).", nameof(slug));
        if (string.IsNullOrWhiteSpace(fieldType))
            throw new ArgumentException("Field type cannot be empty", nameof(fieldType));

        var id = Guid.NewGuid();
        var fieldName = slug;

        return new FieldDefinition(
            id,
            schemaId,
            fieldName,
            label,
            slug,
            fieldType,
            isRequired,
            defaultValue,
            helpText,
            validationRules,
            displayOrder);
    }
}

/// <summary>
/// Domain event raised when a schema is created
/// </summary>
public record SchemaCreatedDomainEvent(
    Guid SchemaId,
    string SchemaName,
    string SchemaType) : DomainEvent;

/// <summary>
/// Domain event raised when a schema is updated
/// </summary>
public record SchemaUpdatedDomainEvent(
    Guid SchemaId,
    string SchemaName,
    int SchemaVersion) : DomainEvent;
