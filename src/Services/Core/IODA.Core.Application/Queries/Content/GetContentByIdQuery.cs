using IODA.Core.Application.DTOs;
using MediatR;

namespace IODA.Core.Application.Queries.Content;

public record GetContentByIdQuery(Guid ContentId) : IRequest<ContentDto?>;
