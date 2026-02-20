namespace IODA.Core.Application.Schemas;

/// <summary>
/// Suggested default fields when creating a new content schema (Req 1 — 006).
/// Frontend and API can use this to prefill the schema designer; all fields are editable/removable before save.
/// </summary>
public static class DefaultSchemaFields
{
    public static IReadOnlyList<DefaultFieldSuggestion> SuggestedFields { get; } = new[]
    {
        new DefaultFieldSuggestion("Título", "title", "text"),
        new DefaultFieldSuggestion("Descripción corta / Teaser", "teaser", "text"),
        new DefaultFieldSuggestion("Imagen", "image", "media"),
        new DefaultFieldSuggestion("Contenido", "content", "richtext"),
    };
}

public record DefaultFieldSuggestion(string Label, string Slug, string FieldType);
