using IODA.Core.Application.Schemas;
using IODA.Core.Domain.Entities;
using IODA.Core.Domain.Exceptions;
using IODA.Core.Domain.Repositories;
using System.Text.Json;

namespace IODA.Core.Application.Commands.Content;

internal static class MediaFieldContentValidator
{
    public static async Task ValidateAsync(
        ContentSchema schema,
        IReadOnlyDictionary<string, object> fields,
        Guid projectId,
        IMediaItemRepository mediaRepository,
        CancellationToken cancellationToken)
    {
        var errors = new List<SchemaValidationErrorEntry>();

        foreach (var fieldDef in schema.Fields.OrderBy(f => f.DisplayOrder))
        {
            var type = FieldTypeCanonicalizer.Canonicalize(fieldDef.FieldType);
            if (!string.Equals(type, "media", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!fields.TryGetValue(fieldDef.Slug, out var rawValue) || IsNullOrEmpty(rawValue))
                continue;

            if (IsMultiValue(rawValue))
            {
                errors.Add(new SchemaValidationErrorEntry(
                    fieldDef.Slug,
                    $"Field '{fieldDef.Slug}' supports a single media item only. Multiple media values are not allowed."));
                continue;
            }

            if (!MediaFieldRulesParser.TryGetMediaId(rawValue, out var mediaId))
            {
                errors.Add(new SchemaValidationErrorEntry(fieldDef.Slug, $"Field '{fieldDef.Slug}' must contain a valid media id."));
                continue;
            }

            var mediaItem = await mediaRepository.GetByIdAsync(mediaId, cancellationToken);
            if (mediaItem is null || mediaItem.ProjectId != projectId)
            {
                errors.Add(new SchemaValidationErrorEntry(fieldDef.Slug, $"Field '{fieldDef.Slug}' references media '{mediaId}' not found in project."));
                continue;
            }

            if (!MediaFieldRulesParser.TryParseValidationRules(fieldDef.ValidationRules, out var rules, out var parseError))
            {
                errors.Add(new SchemaValidationErrorEntry(fieldDef.Slug, parseError ?? "Invalid validationRules.media."));
                continue;
            }

            if (rules is null)
                continue;

            var mediaError = MediaFieldRulesParser.ValidateMediaItem(mediaItem, rules);
            if (!string.IsNullOrWhiteSpace(mediaError))
                errors.Add(new SchemaValidationErrorEntry(fieldDef.Slug, mediaError));
        }

        if (errors.Count > 0)
            throw new SchemaValidationException(errors);
    }

    private static bool IsNullOrEmpty(object? value)
    {
        return value switch
        {
            null => true,
            string s => string.IsNullOrWhiteSpace(s),
            JsonElement je when je.ValueKind == JsonValueKind.Null => true,
            JsonElement je when je.ValueKind == JsonValueKind.Undefined => true,
            JsonElement je when je.ValueKind == JsonValueKind.String => string.IsNullOrWhiteSpace(je.GetString()),
            _ => false
        };
    }

    private static bool IsMultiValue(object? value)
    {
        return value switch
        {
            List<object> => true,
            JsonElement je when je.ValueKind == JsonValueKind.Array => true,
            _ => false
        };
    }
}
