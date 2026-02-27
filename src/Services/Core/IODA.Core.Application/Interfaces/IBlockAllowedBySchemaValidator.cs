using IODA.Core.Domain.Entities;

namespace IODA.Core.Application.Interfaces;

/// <summary>
/// Validates that a block type is allowed by the content's schema and respects min/max occurrences.
/// </summary>
public interface IBlockAllowedBySchemaValidator
{
    /// <summary>
    /// Validates that the schema allows adding a block of the given type and that max occurrences is not exceeded.
    /// </summary>
    void ValidateAdd(Content content, ContentSchema schema, string blockType);

    /// <summary>
    /// Validates that removing a block of the given type would not leave the content below min occurrences for that type.
    /// </summary>
    void ValidateRemove(Content content, ContentSchema schema, string blockType);
}
