using IODA.Core.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace IODA.Core.Infrastructure.Persistence.Converters;

/// <summary>
/// EF Core value converters for Domain Value Objects
/// </summary>
public static class ValueObjectConverters
{
    public static ValueConverter<Slug, string> SlugConverter =>
        new(v => v.Value, v => Slug.Create(v));

    public static ValueConverter<ContentStatus, string> ContentStatusConverter =>
        new(v => v.Value, v => ContentStatus.FromString(v));

    public static ValueConverter<Identifier, string> IdentifierConverter =>
        new(v => v.FullId, v => Identifier.FromString(v));
}
