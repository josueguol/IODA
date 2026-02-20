using IODA.Core.Application.DTOs;
using IODA.Core.Application.Queries.Content;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IODA.Core.API.Controllers;

/// <summary>API de entrega de contenido (Delivery): resolución por path para themes/front. Sin autenticación (Req 5).</summary>
[ApiController]
[Route("api/sites/{siteId:guid}")]
[AllowAnonymous]
public class ContentDeliveryController : ControllerBase
{
    private readonly IMediator _mediator;

    public ContentDeliveryController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Obtener contenido publicado por path. Path se normaliza a slug (ej. "/mi-articulo" → "mi-articulo"). Plantilla simple: /{slug}.</summary>
    [HttpGet("content/by-path")]
    [ProducesResponseType(typeof(ContentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContentDto>> GetByPath(
        Guid siteId,
        [FromQuery] string path,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetContentByPathQuery(siteId, path ?? ""), cancellationToken);
        if (result == null)
            return NotFound();
        return Ok(result);
    }
}
