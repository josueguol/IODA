using IODA.Core.Application.Commands.Hierarchies;
using IODA.Core.Application.DTOs;
using IODA.Core.Application.Queries.Hierarchies;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IODA.Core.API.Controllers;

/// <summary>Módulo Jerarquías: categorías para agrupar contenido (con jerarquía padre-hijos).</summary>
[ApiController]
[Route("api/projects/{projectId:guid}")]
[Authorize(Policy = "content.edit")]
public class HierarchiesController : ControllerBase
{
    private readonly IMediator _mediator;

    public HierarchiesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Listar jerarquías del proyecto (lista plana con ParentHierarchyId).</summary>
    [HttpGet("hierarchies")]
    [ProducesResponseType(typeof(IReadOnlyList<HierarchyDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<HierarchyDto>>> List(Guid projectId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ListHierarchiesByProjectQuery(projectId), cancellationToken);
        return Ok(result);
    }

    /// <summary>Obtener jerarquía por ID.</summary>
    [HttpGet("hierarchies/{hierarchyId:guid}")]
    [ProducesResponseType(typeof(HierarchyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HierarchyDto>> GetById(Guid projectId, Guid hierarchyId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetHierarchyByIdQuery(hierarchyId), cancellationToken);
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    /// <summary>Crear jerarquía. Slug opcional (se deriva del nombre).</summary>
    [HttpPost("hierarchies")]
    [ProducesResponseType(typeof(HierarchyDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<HierarchyDto>> Create(
        Guid projectId,
        [FromBody] CreateHierarchyRequest request,
        CancellationToken cancellationToken)
    {
        var hierarchy = await _mediator.Send(
            new CreateHierarchyCommand(
                projectId,
                request.Name,
                request.Slug ?? string.Empty,
                request.Description,
                request.ImageUrl,
                request.ParentHierarchyId),
            cancellationToken);
        return CreatedAtAction(nameof(GetById), new { projectId, hierarchyId = hierarchy.Id }, hierarchy);
    }

    /// <summary>Actualizar jerarquía.</summary>
    [HttpPut("hierarchies/{hierarchyId:guid}")]
    [ProducesResponseType(typeof(HierarchyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HierarchyDto>> Update(
        Guid projectId,
        Guid hierarchyId,
        [FromBody] UpdateHierarchyRequest request,
        CancellationToken cancellationToken)
    {
        var hierarchy = await _mediator.Send(
            new UpdateHierarchyCommand(
                hierarchyId,
                request.Name,
                request.Slug ?? string.Empty,
                request.Description,
                request.ImageUrl,
                request.ParentHierarchyId),
            cancellationToken);
        return Ok(hierarchy);
    }

    /// <summary>Eliminar jerarquía (solo si no tiene hijos).</summary>
    [HttpDelete("hierarchies/{hierarchyId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Delete(Guid projectId, Guid hierarchyId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteHierarchyCommand(hierarchyId), cancellationToken);
        return NoContent();
    }
}

public record CreateHierarchyRequest(
    string Name,
    string? Slug = null,
    string? Description = null,
    string? ImageUrl = null,
    Guid? ParentHierarchyId = null);

public record UpdateHierarchyRequest(
    string Name,
    string? Slug = null,
    string? Description = null,
    string? ImageUrl = null,
    Guid? ParentHierarchyId = null);
