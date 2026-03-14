using IODA.Core.Application.DTOs;
using MediatR;

namespace IODA.Core.Application.Commands.Media;

public record ReplaceMediaFileCommand(
    Guid ProjectId,
    Guid MediaItemId,
    Stream FileStream,
    string FileName,
    string ContentType,
    long SizeBytes,
    Guid UpdatedBy,
    string? DisplayName = null,
    Dictionary<string, object>? Metadata = null) : IRequest<MediaItemDto>;
