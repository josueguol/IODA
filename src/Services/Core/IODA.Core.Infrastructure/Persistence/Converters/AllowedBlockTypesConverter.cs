using System.Text.Json;
using IODA.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace IODA.Core.Infrastructure.Persistence.Converters;

/// <summary>
/// Converts List&lt;AllowedBlockTypeRule&gt; / IReadOnlyList to/from JSON string for jsonb column.
/// </summary>
public static class AllowedBlockTypesConverter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public static ValueConverter<List<AllowedBlockTypeRule>, string> Instance =>
        new(
            v => JsonSerializer.Serialize(v, JsonOptions),
            v => string.IsNullOrWhiteSpace(v)
                ? new List<AllowedBlockTypeRule>()
                : JsonSerializer.Deserialize<List<AllowedBlockTypeRule>>(v, JsonOptions) ?? new List<AllowedBlockTypeRule>());
}
