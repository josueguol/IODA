using IODA.Core.Application.DTOs;
using MediatR;

namespace IODA.Core.Application.Commands.Content;

public record UnpublishContentCommand(
    Guid ContentId,
    string Reason,
    Guid UnpublishedBy) : IRequest<ContentDto>;
