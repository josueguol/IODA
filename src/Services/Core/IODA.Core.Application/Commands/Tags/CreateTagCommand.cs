using IODA.Core.Application.DTOs;
using MediatR;

namespace IODA.Core.Application.Commands.Tags;

public record CreateTagCommand(Guid ProjectId, string Name, string? Slug = null) : IRequest<TagDto>;
