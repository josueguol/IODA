using IODA.Shared.BuildingBlocks.Domain;

namespace IODA.Core.Domain.ValueObjects;

/// <summary>
/// Value Object representing content status
/// </summary>
public sealed class ContentStatus : ValueObject
{
    public static readonly ContentStatus Draft = new(nameof(Draft));
    public static readonly ContentStatus InReview = new(nameof(InReview));
    public static readonly ContentStatus Approved = new(nameof(Approved));
    public static readonly ContentStatus Published = new(nameof(Published));
    public static readonly ContentStatus Archived = new(nameof(Archived));
    public static readonly ContentStatus Rejected = new(nameof(Rejected));

    public string Value { get; }

    private ContentStatus(string value)
    {
        Value = value;
    }

    public static ContentStatus FromString(string value)
    {
        return value switch
        {
            nameof(Draft) => Draft,
            nameof(InReview) => InReview,
            nameof(Approved) => Approved,
            nameof(Published) => Published,
            nameof(Archived) => Archived,
            nameof(Rejected) => Rejected,
            _ => throw new ArgumentException($"Invalid content status: {value}", nameof(value))
        };
    }

    public bool IsDraft => this == Draft;
    public bool IsPublished => this == Published;
    public bool IsArchived => this == Archived;
    /// <summary>
    /// En el MVP actual del CMS (Draft/Published), permitimos publicar desde Draft.
    /// En un flujo más estricto, solo Approved debería poder publicarse.
    /// </summary>
    public bool CanBePublished => this == Approved || this == Draft;
    public bool CanBeEdited => this == Draft || this == Rejected || this == Published;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(ContentStatus status) => status.Value;
}
