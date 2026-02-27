using IODA.Shared.BuildingBlocks.Domain;

namespace IODA.Core.Domain.Entities;

/// <summary>
/// Bloque de composición dentro de un contenido (hero, text, image, etc.).
/// Entidad hija del agregado Content; no es AggregateRoot.
/// </summary>
public class ContentBlock : Entity<Guid>
{
    public Guid ContentId { get; private set; }
    public string BlockType { get; private set; } = null!;
    public int Order { get; private set; }
    public Dictionary<string, object> Payload { get; private set; } = [];

    // EF Core constructor
    private ContentBlock() { }

    private ContentBlock(Guid id, Guid contentId, string blockType, int order, Dictionary<string, object> payload)
    {
        Id = id;
        ContentId = contentId;
        BlockType = blockType;
        Order = order;
        Payload = payload;
    }

    public static ContentBlock Create(Guid contentId, string blockType, int order, Dictionary<string, object>? payload)
    {
        if (string.IsNullOrWhiteSpace(blockType))
            throw new ArgumentException("Block type cannot be empty.", nameof(blockType));

        return new ContentBlock(
            Guid.NewGuid(),
            contentId,
            blockType.Trim(),
            order,
            payload ?? new Dictionary<string, object>());
    }

    public void SetOrder(int order)
    {
        Order = order;
    }

    public void UpdatePayload(Dictionary<string, object> payload)
    {
        Payload = payload ?? new Dictionary<string, object>();
    }
}
