using IODA.Core.Application.DTOs;
using MediatR;

namespace IODA.Core.Application.Queries.Media;

public record GetMediaByIdQuery(Guid MediaItemId) : IRequest<MediaItemDto?>;
