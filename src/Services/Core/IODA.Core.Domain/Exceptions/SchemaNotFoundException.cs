using IODA.Shared.BuildingBlocks.Domain;

namespace IODA.Core.Domain.Exceptions;

public class SchemaNotFoundException : DomainException
{
    public Guid SchemaId { get; }

    public SchemaNotFoundException(Guid schemaId)
        : base($"Schema with ID '{schemaId}' was not found.")
    {
        SchemaId = schemaId;
    }

    public SchemaNotFoundException(string message) : base(message)
    {
        SchemaId = Guid.Empty;
    }
}
