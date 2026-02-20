using IODA.Core.Application.Commands.Tags;
using IODA.Core.Application.DTOs;
using IODA.Core.Application.Queries.Tags;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IODA.Core.API.Controllers;

[ApiController]
[Route("api/projects/{projectId:guid}")]
[Authorize(Policy = "content.edit")]
public class TagsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TagsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Listar etiquetas del proyecto.</summary>
    [HttpGet("tags")]
    [ProducesResponseType(typeof(IReadOnlyList<TagDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TagDto>>> List(Guid projectId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ListTagsByProjectQuery(projectId), cancellationToken);
        return Ok(result);
    }

    /// <summary>Crear etiqueta en el proyecto. Slug opcional (se deriva del nombre).</summary>
    [HttpPost("tags")]
    [ProducesResponseType(typeof(TagDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TagDto>> Create(
        Guid projectId,
        [FromBody] CreateTagRequest request,
        CancellationToken cancellationToken)
    {
        var tag = await _mediator.Send(
            new CreateTagCommand(projectId, request.Name, request.Slug),
            cancellationToken);
        return CreatedAtAction(nameof(List), new { projectId }, tag);
    }
}

public record CreateTagRequest(string Name, string? Slug = null);
