using IODA.Core.Domain.Entities;
using IODA.Core.Domain.Exceptions;
using IODA.Core.Application.Interfaces;

namespace IODA.Core.Application.Services;

public class BlockAllowedBySchemaValidator : IBlockAllowedBySchemaValidator
{
    public void ValidateAdd(Content content, ContentSchema schema, string blockType)
    {
        if (string.IsNullOrWhiteSpace(blockType))
            throw new BlockTypeNotAllowedException(blockType ?? "", content.Id, "Block type is required.");

        var rules = schema.AllowedBlockTypes;
        if (rules == null || rules.Count == 0)
            throw new BlockTypeNotAllowedException(blockType, content.Id,
                $"The schema '{schema.SchemaName}' does not allow any blocks. Configure allowed block types in the schema.");

        var rule = rules.FirstOrDefault(r => string.Equals(r.BlockType, blockType, StringComparison.OrdinalIgnoreCase));
        if (rule == null)
            throw new BlockTypeNotAllowedException(blockType, content.Id,
                $"Block type '{blockType}' is not allowed for this content. Allowed types: {string.Join(", ", rules.Select(r => r.BlockType))}.");

        if (rule.MaxOccurrences.HasValue)
        {
            var count = content.Blocks.Count(b => string.Equals(b.BlockType, blockType, StringComparison.OrdinalIgnoreCase));
            if (count >= rule.MaxOccurrences.Value)
                throw new BlockTypeNotAllowedException(blockType, content.Id,
                    $"Maximum occurrences ({rule.MaxOccurrences.Value}) of block type '{blockType}' already reached.");
        }
    }

    public void ValidateRemove(Content content, ContentSchema schema, string blockType)
    {
        if (string.IsNullOrWhiteSpace(blockType)) return;

        var rule = schema.AllowedBlockTypes?
            .FirstOrDefault(r => string.Equals(r.BlockType, blockType, StringComparison.OrdinalIgnoreCase));
        if (rule?.MinOccurrences is not { } min) return;

        var count = content.Blocks.Count(b => string.Equals(b.BlockType, blockType, StringComparison.OrdinalIgnoreCase));
        if (count <= min)
            throw new BlockTypeNotAllowedException(blockType, content.Id,
                $"At least {min} block(s) of type '{blockType}' are required. Cannot remove.");
    }
}
