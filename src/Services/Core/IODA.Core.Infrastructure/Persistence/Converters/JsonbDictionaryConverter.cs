using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace IODA.Core.Infrastructure.Persistence.Converters;

/// <summary>
/// Converts Dictionary&lt;string, object&gt; to/from JSON string for PostgreSQL JSONB columns
/// </summary>
public class JsonbDictionaryConverter : ValueConverter<Dictionary<string, object>, string>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public JsonbDictionaryConverter()
        : base(
            v => Serialize(v),
            v => Deserialize(v))
    {
    }

    private static string Serialize(Dictionary<string, object>? value)
    {
        if (value == null || value.Count == 0)
        {
            return "{}";
        }

        return JsonSerializer.Serialize(value, JsonOptions);
    }

    private static Dictionary<string, object> Deserialize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "{}")
        {
            return new Dictionary<string, object>();
        }

        var element = JsonSerializer.Deserialize<JsonElement>(value);
        return JsonElementToDictionary(element);
    }

    private static Dictionary<string, object> JsonElementToDictionary(JsonElement element)
    {
        var dict = new Dictionary<string, object>();

        foreach (var property in element.EnumerateObject())
        {
            dict[property.Name] = JsonElementToObject(property.Value);
        }

        return dict;
    }

    private static object JsonElementToObject(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null!,
            JsonValueKind.Array => element.EnumerateArray()
                .Select(JsonElementToObject)
                .ToList(),
            JsonValueKind.Object => JsonElementToDictionary(element),
            _ => element.GetRawText()
        };
    }
}

/// <summary>
/// Nullable version for Dictionary&lt;string, object&gt;? (e.g. ValidationRules)
/// </summary>
public class NullableJsonbDictionaryConverter : ValueConverter<Dictionary<string, object>?, string>
{
    public NullableJsonbDictionaryConverter()
        : base(
            v => NullableSerialize(v),
            v => NullableDeserialize(v))
    {
    }

    private static string NullableSerialize(Dictionary<string, object>? value)
    {
        if (value == null || value.Count == 0)
        {
            return "{}";
        }

        return JsonSerializer.Serialize(value, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });
    }

    private static Dictionary<string, object>? NullableDeserialize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "{}")
        {
            return null;
        }

        var element = JsonSerializer.Deserialize<JsonElement>(value);
        return NullableJsonElementToDictionary(element);
    }

    private static Dictionary<string, object> NullableJsonElementToDictionary(JsonElement element)
    {
        var dict = new Dictionary<string, object>();
        foreach (var property in element.EnumerateObject())
        {
            dict[property.Name] = NullableJsonElementToObject(property.Value);
        }

        return dict;
    }

    private static object NullableJsonElementToObject(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null!,
            JsonValueKind.Array => element.EnumerateArray().Select(NullableJsonElementToObject).ToList(),
            JsonValueKind.Object => NullableJsonElementToDictionary(element),
            _ => element.GetRawText()
        };
    }
}
