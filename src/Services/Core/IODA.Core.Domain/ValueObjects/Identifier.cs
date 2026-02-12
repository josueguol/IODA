using IODA.Shared.BuildingBlocks.Domain;

namespace IODA.Core.Domain.ValueObjects;

/// <summary>
/// Value Object representing a unique identifier with prefix
/// Useful for public-facing IDs (e.g., "cnt_abc123", "prj_xyz789")
/// </summary>
public sealed class Identifier : ValueObject
{
    private const int ID_LENGTH = 16;

    public string Prefix { get; }
    public string Value { get; }
    public string FullId => $"{Prefix}_{Value}";

    private Identifier(string prefix, string value)
    {
        Prefix = prefix;
        Value = value;
    }

    /// <summary>
    /// Creates a new Identifier with a random value
    /// </summary>
    public static Identifier Create(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            throw new ArgumentException("Prefix cannot be empty", nameof(prefix));
        }

        if (prefix.Length > 5)
        {
            throw new ArgumentException("Prefix cannot exceed 5 characters", nameof(prefix));
        }

        var value = GenerateRandomId();
        return new Identifier(prefix, value);
    }

    /// <summary>
    /// Creates an Identifier from a full ID string (e.g., "cnt_abc123")
    /// </summary>
    public static Identifier FromString(string fullId)
    {
        if (string.IsNullOrWhiteSpace(fullId))
        {
            throw new ArgumentException("Full ID cannot be empty", nameof(fullId));
        }

        var parts = fullId.Split('_', 2);
        if (parts.Length != 2)
        {
            throw new ArgumentException("Invalid identifier format. Expected 'prefix_value'", nameof(fullId));
        }

        return new Identifier(parts[0], parts[1]);
    }

    private static string GenerateRandomId()
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, ID_LENGTH)
            .Select(s => s[random.Next(s.Length)])
            .ToArray());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Prefix;
        yield return Value;
    }

    public override string ToString() => FullId;

    public static implicit operator string(Identifier identifier) => identifier.FullId;
}
