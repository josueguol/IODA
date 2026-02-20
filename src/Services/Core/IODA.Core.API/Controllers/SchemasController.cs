using IODA.Core.Application.Commands.Schemas;
using IODA.Core.Application.DTOs;
using IODA.Core.Application.Queries.Schemas;
using IODA.Core.Application.Schemas;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IODA.Core.API.Controllers;

[ApiController]
[Route("api/projects/{projectId:guid}/schemas")]
[Authorize(Policy = "schema.edit")]
public class SchemasController : ControllerBase
{
    private readonly IMediator _mediator;

    public SchemasController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Crear un schema de contenido en el proyecto.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Guid>> Create(
        Guid projectId,
        [FromBody] CreateSchemaRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateContentSchemaCommand(
            projectId,
            request.SchemaName,
            request.SchemaType,
            request.Description,
            request.Fields,
            request.CreatedBy,
            request.ParentSchemaId);
        var id = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { projectId, schemaId = id }, id);
    }

    /// <summary>Campos sugeridos al crear un nuevo schema (title, teaser, image, content). Son editables y eliminables antes de guardar.</summary>
    [HttpGet("default-fields")]
    [ProducesResponseType(typeof(IReadOnlyList<DefaultFieldSuggestionDto>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<DefaultFieldSuggestionDto>> GetDefaultFields(Guid projectId)
    {
        var list = DefaultSchemaFields.SuggestedFields
            .Select(f => new DefaultFieldSuggestionDto(f.Label, f.Slug, f.FieldType))
            .ToList();
        return Ok(list);
    }

    /// <summary>Obtener un schema por ID.</summary>
    [HttpGet("{schemaId:guid}")]
    [ProducesResponseType(typeof(ContentSchemaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContentSchemaDto>> GetById(
        Guid projectId,
        Guid schemaId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSchemaByIdQuery(schemaId), cancellationToken);
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    /// <summary>Listar schemas del proyecto.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ContentSchemaListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ContentSchemaListItemDto>>> List(
        Guid projectId,
        [FromQuery] bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new ListSchemasByProjectQuery(projectId, activeOnly),
            cancellationToken);
        return Ok(result);
    }
}

public record DefaultFieldSuggestionDto(string Label, string Slug, string FieldType);

public record CreateSchemaRequest(
    string SchemaName,
    string SchemaType,
    string? Description,
    List<CreateSchemaFieldDto> Fields,
    Guid CreatedBy,
    Guid? ParentSchemaId = null);
