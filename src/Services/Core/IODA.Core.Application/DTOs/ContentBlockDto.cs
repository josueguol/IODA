namespace IODA.Core.Application.DTOs;

public record ContentBlockDto(
    Guid Id,
    Guid ContentId,
    string BlockType,
    int Order,
    Dictionary<string, object> Payload);
