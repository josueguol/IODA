using IODA.Shared.BuildingBlocks.Domain;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace IODA.Core.Domain.ValueObjects;

/// <summary>
/// Value Object representing a URL-friendly slug
/// </summary>
public sealed class Slug : ValueObject
{
    private const int MAX_LENGTH = 200;
    private static readonly Regex AllowedSlugRegex = new("^[a-z0-9][a-z0-9_-]*$", RegexOptions.Compiled);

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

        var normalized = NormalizeInput(value);

        if (normalized.Length > MAX_LENGTH)
        {
            throw new ArgumentException($"Slug cannot exceed {MAX_LENGTH} characters", nameof(value));
        }

        if (!AllowedSlugRegex.IsMatch(normalized))
        {
            throw new ArgumentException("Slug contains invalid characters. Allowed: a-z, 0-9, -, _", nameof(value));
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

        var slug = BuildSlugFromTitle(title);

        if (slug.Length > MAX_LENGTH)
        {
            slug = slug[..MAX_LENGTH].TrimEnd('-');
        }

        if (slug.Length == 0)
        {
            throw new ArgumentException("Title does not contain valid slug characters", nameof(title));
        }

        return new Slug(slug);
    }

    private static string NormalizeInput(string value)
    {
        return RemoveDiacritics(value)
            .ToLowerInvariant()
            .Trim();
    }

    private static string BuildSlugFromTitle(string title)
    {
        var normalized = NormalizeInput(title);
        var builder = new StringBuilder(normalized.Length);
        var lastWasDash = false;

        foreach (var ch in normalized)
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
                lastWasDash = false;
                continue;
            }

            if (ch is '-' or '_' || char.IsWhiteSpace(ch))
            {
                if (!lastWasDash && builder.Length > 0)
                {
                    builder.Append('-');
                    lastWasDash = true;
                }
            }
        }

        return builder.ToString().Trim('-');
    }

    private static string RemoveDiacritics(string input)
    {
        var normalized = input.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(ch);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(Slug slug) => slug.Value;
}
