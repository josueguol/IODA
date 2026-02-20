using IODA.Core.Application.DTOs;
using MediatR;

namespace IODA.Core.Application.Queries.Content;

/// <summary>Resuelve contenido publicado por sitio y path (Req 5). Path se normaliza a slug para plantilla /{slug}.</summary>
public record GetContentByPathQuery(Guid SiteId, string Path) : IRequest<ContentDto?>;
