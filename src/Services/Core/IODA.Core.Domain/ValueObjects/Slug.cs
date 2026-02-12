using IODA.Shared.BuildingBlocks.Domain;

namespace IODA.Core.Domain.ValueObjects;

/// <summary>
/// Value Object representing a URL-friendly slug
/// </summary>
public sealed class Slug : ValueObject
{
    private const int MAX_LENGTH = 200;
    private static readonly char[] INVALID_CHARS = ['/', '\\', '?', '#', '[', ']', '@', '!', '$', '&', '\'', '(', ')', '*', '+', ',', ';', '=', ' ', '%', '<', '>', '"', '{', '}', '|', '^', '`'];

    public string Value { get; }

    private Slug(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new Slug from a string value
    /// </summary>
    public static Slug Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Slug cannot be empty", nameof(value));
        }

        var normalized = Normalize(value);

        if (normalized.Length > MAX_LENGTH)
        {
            throw new ArgumentException($"Slug cannot exceed {MAX_LENGTH} characters", nameof(value));
        }

        if (ContainsInvalidCharacters(normalized))
        {
            throw new ArgumentException("Slug contains invalid characters", nameof(value));
        }

        return new Slug(normalized);
    }

    /// <summary>
    /// Creates a slug from a title, normalizing it automatically
    /// </summary>
    public static Slug FromTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title cannot be empty", nameof(title));
        }

        var slug = title
            .ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-");

        // Remove invalid characters
        foreach (var ch in INVALID_CHARS)
        {
            slug = slug.Replace(ch.ToString(), string.Empty);
        }

        // Remove consecutive hyphens
        while (slug.Contains("--"))
        {
            slug = slug.Replace("--", "-");
        }

        // Trim hyphens from start and end
        slug = slug.Trim('-');

        if (slug.Length > MAX_LENGTH)
        {
            slug = slug[..MAX_LENGTH].TrimEnd('-');
        }

        return new Slug(slug);
    }

    private static string Normalize(string value)
    {
        return value.ToLowerInvariant().Trim();
    }

    private static bool ContainsInvalidCharacters(string value)
    {
        return value.Any(c => INVALID_CHARS.Contains(c));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(Slug slug) => slug.Value;
}
