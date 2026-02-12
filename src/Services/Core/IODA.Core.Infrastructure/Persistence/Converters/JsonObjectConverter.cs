using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace IODA.Core.Infrastructure.Persistence.Converters;

/// <summary>
/// Converts object? to/from JSON string for flexible storage (e.g. default values)
/// </summary>
public class JsonObjectConverter : ValueConverter<object?, string?>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public JsonObjectConverter()
        : base(
            v => Serialize(v),
            v => Deserialize(v))
    {
    }

    private static string? Serialize(object? value)
    {
        if (value == null)
        {
            return null;
        }

        return JsonSerializer.Serialize(value, JsonOptions);
    }

    private static object? Deserialize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var element = JsonSerializer.Deserialize<JsonElement>(value);
        return JsonElementToObject(element);
    }

    private static object? JsonElementToObject(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => element.EnumerateArray().Select(JsonElementToObject).ToList(),
            JsonValueKind.Object => element,
            _ => element.GetRawText()
        };
    }
}
