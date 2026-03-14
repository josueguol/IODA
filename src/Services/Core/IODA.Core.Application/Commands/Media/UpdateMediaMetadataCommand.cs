using IODA.Core.Application.DTOs;
using MediatR;

namespace IODA.Core.Application.Commands.Media;

public record UpdateMediaMetadataCommand(
    Guid ProjectId,
    Guid MediaItemId,
    Guid UpdatedBy,
    string? DisplayName,
    Dictionary<string, object>? Metadata) : IRequest<MediaItemDto>;
