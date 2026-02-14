using IODA.Publishing.API.Contracts;
using IODA.Publishing.Application.Commands;
using IODA.Publishing.Application.Queries;
using IODA.Publishing.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IODA.Publishing.API.Controllers;

/// <summary>
/// API de publicación: solicitudes, aprobación y rechazo. Todos los endpoints requieren JWT (rol Editor o Admin).
/// </summary>
[ApiController]
[Route("api/publishing")]
[Authorize(Policy = "Editor")]
public class PublishingController : ControllerBase
{
    private readonly IMediator _mediator;

    public PublishingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Solicitar publicación de contenido.</summary>
    [HttpPost("requests")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> RequestPublication([FromBody] RequestPublicationRequest request, CancellationToken cancellationToken)
    {
        var command = new RequestPublicationCommand(
            request.ContentId,
            request.ProjectId,
            request.EnvironmentId,
            request.RequestedBy);
        var id = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetPublicationRequests), new { }, new { id });
    }

    /// <summary>Aprobar publicación: valida contenido y llama al Core API para publicar.</summary>
    [HttpPost("requests/{requestId:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApprovePublication(Guid requestId, [FromBody] ApprovePublicationRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new ApprovePublicationCommand(requestId, request.ApprovedBy), cancellationToken);
        return NoContent();
    }

    /// <summary>Rechazar solicitud de publicación.</summary>
    [HttpPost("requests/{requestId:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectPublication(Guid requestId, [FromBody] RejectPublicationRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new RejectPublicationCommand(requestId, request.RejectedBy, request.Reason), cancellationToken);
        return NoContent();
    }

    /// <summary>Listar solicitudes de publicación (por contentId, status o todas). Status: Pending | Approved | Rejected.</summary>
    [HttpGet("requests")]
    [ProducesResponseType(typeof(IReadOnlyList<PublicationRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<PublicationRequestDto>>> GetPublicationRequests(
        [FromQuery] Guid? contentId = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        PublicationRequestStatus? statusEnum = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<PublicationRequestStatus>(status, ignoreCase: true, out var parsed))
                return BadRequest(new ProblemDetails { Status = 400, Title = "Bad Request", Detail = "Invalid status. Allowed values: Pending, Approved, Rejected." });
            statusEnum = parsed;
        }
        var result = await _mediator.Send(new GetPublicationRequestsQuery(contentId, statusEnum), cancellationToken);
        return Ok(result);
    }
}
