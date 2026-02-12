using MediatR;

namespace IODA.Core.Application.Commands.Media;

public record UploadMediaCommand(
    Guid ProjectId,
    Stream FileStream,
    string FileName,
    string ContentType,
    long SizeBytes,
    Guid CreatedBy,
    string? DisplayName = null,
    Dictionary<string, object>? Metadata = null) : IRequest<Guid>;
