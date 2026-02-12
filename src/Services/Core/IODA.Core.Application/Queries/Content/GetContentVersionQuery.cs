using IODA.Core.Application.DTOs;
using MediatR;

namespace IODA.Core.Application.Queries.Content;

public record GetContentVersionQuery(Guid ContentId, int VersionNumber) : IRequest<ContentVersionDto?>;
