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
        if (_fields.Any(f => f.FieldName == field.FieldName))
        {
            throw new InvalidOperationException($"Field '{field.FieldName}' already exists");
        }

        _fields.Add(field);
        SchemaVersion++;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new SchemaUpdatedDomainEvent(Id, SchemaName, SchemaVersion));
    }

    public void RemoveField(string fieldName)
    {
        var field = _fields.FirstOrDefault(f => f.FieldName == fieldName);
        if (field == null)
        {
            throw new InvalidOperationException($"Field '{fieldName}' not found");
        }

        _fields.Remove(field);
        SchemaVersion++;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new SchemaUpdatedDomainEvent(Id, SchemaName, SchemaVersion));
    }

    public void UpdateField(string fieldName, FieldDefinition updatedField)
    {
        var field = _fields.FirstOrDefault(f => f.FieldName == fieldName);
        if (field == null)
        {
            throw new InvalidOperationException($"Field '{fieldName}' not found");
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
/// Defines a field within a schema
/// </summary>
public class FieldDefinition : Entity<Guid>
{
    public Guid SchemaId { get; private set; }
    public string FieldName { get; private set; } = null!;
    public string FieldType { get; private set; } = null!;
    public bool IsRequired { get; private set; }
    public object? DefaultValue { get; private set; }
    public string? HelpText { get; private set; }
    
    /// <summary>
    /// Validation rules stored as JSON (e.g., min/max length, regex pattern, etc.)
    /// </summary>
    public Dictionary<string, object>? ValidationRules { get; private set; }
    
    public int DisplayOrder { get; private set; }

    // Navigation
    public ContentSchema Schema { get; private set; } = null!;

    // EF Core constructor
    private FieldDefinition() { }

    private FieldDefinition(
        Guid id,
        Guid schemaId,
        string fieldName,
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
        FieldType = fieldType;
        IsRequired = isRequired;
        DefaultValue = defaultValue;
        HelpText = helpText;
        ValidationRules = validationRules;
        DisplayOrder = displayOrder;
    }

    /// <summary>
    /// Sets the schema id (used when creating schema with fields - pass Guid.Empty initially).
    /// </summary>
    internal void SetSchemaId(Guid schemaId)
    {
        SchemaId = schemaId;
    }

    public static FieldDefinition Create(
        Guid schemaId,
        string fieldName,
        string fieldType,
        bool isRequired = false,
        object? defaultValue = null,
        string? helpText = null,
        Dictionary<string, object>? validationRules = null,
        int displayOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            throw new ArgumentException("Field name cannot be empty", nameof(fieldName));
        }

        if (string.IsNullOrWhiteSpace(fieldType))
        {
            throw new ArgumentException("Field type cannot be empty", nameof(fieldType));
        }

        var id = Guid.NewGuid();

        return new FieldDefinition(
            id,
            schemaId,
            fieldName,
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
