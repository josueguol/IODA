namespace IODA.Shared.BuildingBlocks.Domain;

/// <summary>
/// Base class for aggregate roots
/// An aggregate root is an entity that is the entry point to an aggregate
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    // Aggregate roots may have additional behavior
    // For now, inherits everything from Entity
}
