using System.Text.Json;
using System.Text.RegularExpressions;
using IODA.Core.Application.Interfaces;
using IODA.Core.Domain.Entities;

namespace IODA.Core.Application.Validators.Schema;

/// <summary>
/// Valida payloads del tipo richtexteditor.
/// Formato esperado: JSON string con { format, markdown, blocks?, metadata? }.
/// </summary>
public sealed class BlockNoteMarkdownFieldValidator : IFieldValidator
{
    private static readonly string[] Types = { "richtexteditor" };
    private static readonly Regex EmbedUrlRegex = new(@"https?://(?<host>[^/\s]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly HashSet<string> DefaultAllowedEmbedHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "www.youtube.com",
        "youtube.com",
        "youtu.be",
        "player.vimeo.com",
        "vimeo.com",
        "www.instagram.com",
        "instagram.com",
        "www.tiktok.com",
        "tiktok.com",
        "twitter.com",
        "x.com",
    };

    public bool CanValidate(string fieldType) =>
        Types.Contains(fieldType.Trim().ToLowerInvariant());

    public IReadOnlyList<SchemaValidationError> Validate(FieldDefinition fieldDef, object? value)
    {
        var errors = new List<SchemaValidationError>();
        if (value == null)
            return errors;

        var payload = CoerceToString(value);
        if (string.IsNullOrWhiteSpace(payload))
            return errors;

        var maxBytes = ResolveMaxBytes(fieldDef.ValidationRules);
        if (payload.Length > maxBytes)
        {
            errors.Add(new SchemaValidationError(fieldDef.Slug, $"Field '{fieldDef.Slug}' exceeds max payload length ({maxBytes})."));
            return errors;
        }

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(payload);
        }
        catch (JsonException)
        {
            errors.Add(new SchemaValidationError(fieldDef.Slug, $"Field '{fieldDef.Slug}' must be a JSON object payload."));
            return errors;
        }

        using (doc)
        {
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                errors.Add(new SchemaValidationError(fieldDef.Slug, $"Field '{fieldDef.Slug}' must be a JSON object."));
                return errors;
            }

            if (!doc.RootElement.TryGetProperty("markdown", out var markdownElement) || markdownElement.ValueKind != JsonValueKind.String)
            {
                errors.Add(new SchemaValidationError(fieldDef.Slug, $"Field '{fieldDef.Slug}' must include 'markdown' as string."));
                return errors;
            }

            var markdown = markdownElement.GetString() ?? string.Empty;
            ValidateMarkdownRules(fieldDef, markdown, errors);

            ValidateEmbedHosts(fieldDef, doc.RootElement, errors);
        }

        return errors;
    }

    private static string? CoerceToString(object value) =>
        value switch
        {
            string s => s,
            JsonElement je when je.ValueKind == JsonValueKind.String => je.GetString(),
            JsonElement je => je.GetRawText(),
            _ => value.ToString()
        };

    private static int ResolveMaxBytes(Dictionary<string, object>? validationRules)
    {
        const int defaultMax = 200_000;
        if (validationRules == null)
            return defaultMax;
        if (!validationRules.TryGetValue("maxPayloadLength", out var maxObj))
            return defaultMax;
        return SchemaValidationHelpers.TryGetInt(maxObj, out var max) && max > 0 ? max : defaultMax;
    }

    private static void ValidateEmbedHosts(FieldDefinition fieldDef, JsonElement root, List<SchemaValidationError> errors)
    {
        var allowedHosts = ResolveAllowedEmbedHosts(fieldDef.ValidationRules);
        if (allowedHosts.Count == 0)
            return;

        if (!root.TryGetProperty("metadata", out var metadata) || metadata.ValueKind != JsonValueKind.Object)
            return;
        if (!metadata.TryGetProperty("embeds", out var embeds) || embeds.ValueKind != JsonValueKind.Array)
            return;

        foreach (var embed in embeds.EnumerateArray())
        {
            if (embed.ValueKind != JsonValueKind.Object)
                continue;
            if (!embed.TryGetProperty("url", out var urlNode) || urlNode.ValueKind != JsonValueKind.String)
                continue;

            var url = urlNode.GetString();
            if (string.IsNullOrWhiteSpace(url))
                continue;
            var host = GetHost(url);
            if (host == null)
                continue;

            var isAllowed = allowedHosts.Contains(host) || allowedHosts.Contains(host.Replace("www.", string.Empty, StringComparison.OrdinalIgnoreCase));
            if (!isAllowed)
                errors.Add(new SchemaValidationError(fieldDef.Slug, $"Field '{fieldDef.Slug}' contains embed host '{host}' which is not allowed."));
        }
    }

    private static void ValidateMarkdownRules(FieldDefinition fieldDef, string markdown, List<SchemaValidationError> errors)
    {
        if (fieldDef.ValidationRules == null)
            return;

        if (fieldDef.ValidationRules.TryGetValue("minLength", out var minLenObj)
            && SchemaValidationHelpers.TryGetInt(minLenObj, out var minLen)
            && markdown.Length < minLen)
        {
            errors.Add(new SchemaValidationError(fieldDef.Slug, $"Field '{fieldDef.Slug}' markdown must be at least {minLen} characters."));
        }

        if (fieldDef.ValidationRules.TryGetValue("maxLength", out var maxLenObj)
            && SchemaValidationHelpers.TryGetInt(maxLenObj, out var maxLen)
            && markdown.Length > maxLen)
        {
            errors.Add(new SchemaValidationError(fieldDef.Slug, $"Field '{fieldDef.Slug}' markdown must not exceed {maxLen} characters."));
        }
    }

    private static string? GetHost(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && !string.IsNullOrWhiteSpace(uri.Host))
            return uri.Host;

        var match = EmbedUrlRegex.Match(url);
        return match.Success ? match.Groups["host"].Value : null;
    }

    private static HashSet<string> ResolveAllowedEmbedHosts(Dictionary<string, object>? validationRules)
    {
        if (validationRules == null || !validationRules.TryGetValue("allowedEmbedHosts", out var hostsObj))
            return DefaultAllowedEmbedHosts;

        var hosts = SchemaValidationHelpers.GetAllowedValuesList(hostsObj)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToLowerInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return hosts.Count > 0 ? hosts : DefaultAllowedEmbedHosts;
    }
}
