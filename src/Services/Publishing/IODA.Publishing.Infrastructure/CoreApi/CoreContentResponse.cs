using System.Text.Json.Serialization;

namespace IODA.Publishing.Infrastructure.CoreApi;

/// <summary>
/// DTO para la respuesta del Core API al obtener contenido (evita referencia a Core.Application).
/// </summary>
public record CoreContentResponse(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("contentType")] string ContentType,
    [property: JsonPropertyName("fields")] Dictionary<string, object>? Fields);
