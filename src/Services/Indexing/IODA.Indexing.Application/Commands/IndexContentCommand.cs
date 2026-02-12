using MediatR;

namespace IODA.Indexing.Application.Commands;

public record IndexContentCommand(
    Guid ContentId,
    Guid VersionId,
    string Title,
    string ContentType,
    DateTime PublishedAt,
    IReadOnlyDictionary<string, object>? Fields = null
) : IRequest;
