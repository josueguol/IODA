using IODA.Core.Application.DTOs;
using MediatR;

namespace IODA.Core.Application.Commands.Content;

public record PublishContentCommand(
    Guid ContentId,
    Guid PublishedBy) : IRequest<ContentDto>;
