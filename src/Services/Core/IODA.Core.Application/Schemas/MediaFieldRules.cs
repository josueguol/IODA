using System.Text.Json;
using IODA.Core.Domain.Entities;

namespace IODA.Core.Application.Schemas;

public sealed record MediaFieldRules(
    HashSet<string> AllowedCategories,
    HashSet<string> AllowedMimeTypes,
    HashSet<string> AllowedExtensions,
    long? MaxSizeBytes);

public static class MediaFieldRulesParser
{
    private static readonly HashSet<string> SupportedCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "image",
        "video",
        "audio",
    };

    private static readonly Dictionary<string, string[]> DefaultMimeTypesByCategory = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image"] = ["image/jpeg", "image/png", "image/webp", "image/svg+xml"],
        ["video"] = ["video/mp4", "video/webm", "video/quicktime"],
        ["audio"] = ["audio/mpeg", "audio/wav", "audio/ogg"],
    };

    private static readonly Dictionary<string, string[]> DefaultExtensionsByCategory = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image"] = ["jpg", "jpeg", "png", "webp", "svg"],
        ["video"] = ["mp4", "webm", "mov"],
        ["audio"] = ["mp3", "wav", "ogg"],
    };

    public static bool TryParseValidationRules(
        Dictionary<string, object>? validationRules,
        out MediaFieldRules? rules,
        out string? error)
    {
        rules = null;
        error = null;

        if (validationRules is null || !validationRules.TryGetValue("media", out var mediaRaw) || mediaRaw is null)
            return true;

        var mediaElement = ToJsonElement(mediaRaw);
        if (mediaElement.ValueKind != JsonValueKind.Object)
        {
            error = "validationRules.media must be an object.";
            return false;
        }

        var allowedCategories = ReadStringSet(mediaElement, "allowedCategories", normalize: NormalizeCategory);
        if (allowedCategories.Count == 0)
        {
            error = "validationRules.media.allowedCategories must include at least one category (image, video, audio).";
            return false;
        }

        var invalidCategories = allowedCategories.Where(c => !SupportedCategories.Contains(c)).ToList();
        if (invalidCategories.Count > 0)
        {
            error = $"validationRules.media.allowedCategories contains invalid values: {string.Join(", ", invalidCategories)}.";
            return false;
        }

        var inferredMimeTypes = allowedCategories
            .SelectMany(c => DefaultMimeTypesByCategory[c])
            .Select(NormalizeMimeType)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var allowedMimeTypes = ReadStringSet(mediaElement, "allowedMimeTypes", normalize: NormalizeMimeType);
        if (allowedMimeTypes.Count == 0)
            allowedMimeTypes = inferredMimeTypes;
        else if (allowedMimeTypes.Any(m => !inferredMimeTypes.Contains(m)))
        {
            error = "validationRules.media.allowedMimeTypes contains values outside the selected categories.";
            return false;
        }

        var inferredExtensions = allowedCategories
            .SelectMany(c => DefaultExtensionsByCategory[c])
            .Select(NormalizeExtension)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var allowedExtensions = ReadStringSet(mediaElement, "allowedExtensions", normalize: NormalizeExtension);
        if (allowedExtensions.Count == 0)
            allowedExtensions = inferredExtensions;
        else if (allowedExtensions.Any(e => !inferredExtensions.Contains(e)))
        {
            error = "validationRules.media.allowedExtensions contains values outside the selected categories.";
            return false;
        }

        long? maxSizeBytes = null;
        if (mediaElement.TryGetProperty("maxSizeBytes", out var maxSizeElement) && maxSizeElement.ValueKind != JsonValueKind.Null)
        {
            if (!TryReadInt64(maxSizeElement, out var parsedMaxSize) || parsedMaxSize <= 0)
            {
                error = "validationRules.media.maxSizeBytes must be a positive integer.";
                return false;
            }
            maxSizeBytes = parsedMaxSize;
        }

        rules = new MediaFieldRules(allowedCategories, allowedMimeTypes, allowedExtensions, maxSizeBytes);
        return true;
    }

    public static bool TryGetMediaId(object? rawValue, out Guid mediaId)
    {
        mediaId = Guid.Empty;
        if (rawValue is null) return false;

        switch (rawValue)
        {
            case Guid g:
                mediaId = g;
                return true;
            case string s:
                return Guid.TryParse(s, out mediaId);
            case JsonElement je when je.ValueKind == JsonValueKind.String:
                return Guid.TryParse(je.GetString(), out mediaId);
            case JsonElement je when je.ValueKind == JsonValueKind.Object:
            {
                if (!je.TryGetProperty("id", out var idEl) || idEl.ValueKind != JsonValueKind.String)
                    return false;
                return Guid.TryParse(idEl.GetString(), out mediaId);
            }
            default:
                return false;
        }
    }

    public static string? ValidateMediaItem(MediaItem item, MediaFieldRules rules)
    {
        var normalizedMimeType = NormalizeMimeType(item.ContentType);
        var category = GetCategoryFromMimeType(normalizedMimeType);

        if (category is null || !rules.AllowedCategories.Contains(category))
            return $"Media category for '{item.FileName}' is not allowed.";

        if (!rules.AllowedMimeTypes.Contains(normalizedMimeType))
            return $"MIME type '{item.ContentType}' is not allowed.";

        var extension = NormalizeExtension(Path.GetExtension(item.FileName));
        if (string.IsNullOrWhiteSpace(extension) || !rules.AllowedExtensions.Contains(extension))
            return $"File extension '{Path.GetExtension(item.FileName)}' is not allowed.";

        if (rules.MaxSizeBytes.HasValue && item.SizeBytes > rules.MaxSizeBytes.Value)
            return $"File size exceeds field maxSizeBytes ({rules.MaxSizeBytes.Value} bytes).";

        return null;
    }

    private static HashSet<string> ReadStringSet(JsonElement parent, string propertyName, Func<string, string> normalize)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!parent.TryGetProperty(propertyName, out var element) || element.ValueKind == JsonValueKind.Null)
            return result;

        if (element.ValueKind != JsonValueKind.Array)
            return result;

        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String) continue;
            var raw = item.GetString();
            if (string.IsNullOrWhiteSpace(raw)) continue;
            result.Add(normalize(raw));
        }

        return result;
    }

    private static JsonElement ToJsonElement(object raw)
    {
        if (raw is JsonElement jsonElement)
            return jsonElement;

        if (raw is string str)
        {
            try
            {
                return JsonDocument.Parse(str).RootElement.Clone();
            }
            catch
            {
                return JsonSerializer.SerializeToElement(raw);
            }
        }

        return JsonSerializer.SerializeToElement(raw);
    }

    private static string NormalizeCategory(string value) => value.Trim().ToLowerInvariant();

    private static string NormalizeMimeType(string value) => value.Trim().ToLowerInvariant();

    private static string NormalizeExtension(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        return normalized.TrimStart('.');
    }

    private static string? GetCategoryFromMimeType(string mimeType)
    {
        if (mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) return "image";
        if (mimeType.StartsWith("video/", StringComparison.OrdinalIgnoreCase)) return "video";
        if (mimeType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase)) return "audio";
        return null;
    }

    private static bool TryReadInt64(JsonElement element, out long value)
    {
        value = 0;
        if (element.ValueKind == JsonValueKind.Number)
            return element.TryGetInt64(out value);

        if (element.ValueKind == JsonValueKind.String)
            return long.TryParse(element.GetString(), out value);

        return false;
    }
}
