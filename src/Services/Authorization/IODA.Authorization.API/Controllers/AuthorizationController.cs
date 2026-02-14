using IODA.Authorization.Application.Commands;
using IODA.Authorization.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IODA.Authorization.API.Controllers;

/// <summary>
/// API de autorización: permisos, roles y reglas de acceso.
/// Todos los endpoints requieren autenticación JWT. CRUD de roles/permisos/reglas requiere rol Admin.
/// </summary>
[ApiController]
[Route("api/authorization")]
[Authorize]
public class AuthorizationController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthorizationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Comprobar si un usuario tiene un permiso en el contexto dado.</summary>
    [HttpPost("check")]
    [ProducesResponseType(typeof(CheckAccessResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CheckAccessResult>> CheckAccess([FromBody] CheckAccessRequest request, CancellationToken cancellationToken)
    {
        var query = new CheckAccessQuery(
            request.UserId,
            request.PermissionCode,
            request.ProjectId,
            request.EnvironmentId,
            request.SchemaId,
            request.ContentStatus);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Listar todos los roles.</summary>
    [HttpGet("roles")]
    [ProducesResponseType(typeof(IReadOnlyList<RoleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> GetRoles(CancellationToken cancellationToken)
    {
        var roles = await _mediator.Send(new GetRolesQuery(), cancellationToken);
        return Ok(roles);
    }

    /// <summary>Crear un rol. Requiere rol Admin.</summary>
    [HttpPost("roles")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Guid>> CreateRole([FromBody] CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateRoleCommand(request.Name, request.Description);
        var roleId = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetRoles), new { id = roleId }, roleId);
    }

    /// <summary>Asignar permisos a un rol. Requiere rol Admin.</summary>
    [HttpPost("roles/{roleId:guid}/permissions")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignPermissionsToRole(Guid roleId, [FromBody] AssignPermissionsRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new AssignPermissionsToRoleCommand(roleId, request.PermissionIds), cancellationToken);
        return NoContent();
    }

    /// <summary>Listar todos los permisos.</summary>
    [HttpGet("permissions")]
    [ProducesResponseType(typeof(IReadOnlyList<PermissionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PermissionDto>>> GetPermissions(CancellationToken cancellationToken)
    {
        var permissions = await _mediator.Send(new GetPermissionsQuery(), cancellationToken);
        return Ok(permissions);
    }

    /// <summary>Crear un permiso. Requiere rol Admin.</summary>
    [HttpPost("permissions")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Guid>> CreatePermission([FromBody] CreatePermissionRequest request, CancellationToken cancellationToken)
    {
        var command = new CreatePermissionCommand(request.Code, request.Description);
        var permissionId = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetPermissions), new { id = permissionId }, permissionId);
    }

    /// <summary>Obtener reglas de acceso de un usuario.</summary>
    [HttpGet("users/{userId:guid}/rules")]
    [ProducesResponseType(typeof(IReadOnlyList<AccessRuleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AccessRuleDto>>> GetUserAccessRules(Guid userId, CancellationToken cancellationToken)
    {
        var rules = await _mediator.Send(new GetUserAccessRulesQuery(userId), cancellationToken);
        return Ok(rules);
    }

    /// <summary>Asignar un rol a un usuario en un ámbito opcional. Requiere rol Admin.</summary>
    [HttpPost("rules")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Guid>> CreateAccessRule([FromBody] CreateAccessRuleRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateAccessRuleCommand(
            request.UserId,
            request.RoleId,
            request.ProjectId,
            request.EnvironmentId,
            request.SchemaId,
            request.ContentStatus);
        var ruleId = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetUserAccessRules), new { userId = request.UserId }, new { id = ruleId });
    }

    /// <summary>Revocar una regla de acceso. Requiere rol Admin.</summary>
    [HttpDelete("rules/{ruleId:guid}")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeAccessRule(Guid ruleId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new RevokeAccessRuleCommand(ruleId), cancellationToken);
        return NoContent();
    }
}

public record CheckAccessRequest(
    Guid UserId,
    string PermissionCode,
    Guid? ProjectId = null,
    Guid? EnvironmentId = null,
    Guid? SchemaId = null,
    string? ContentStatus = null);

public record CreateRoleRequest(string Name, string Description = "");

public record AssignPermissionsRequest(IReadOnlyList<Guid> PermissionIds);

public record CreatePermissionRequest(string Code, string Description = "");

public record CreateAccessRuleRequest(
    Guid UserId,
    Guid RoleId,
    Guid? ProjectId = null,
    Guid? EnvironmentId = null,
    Guid? SchemaId = null,
    string? ContentStatus = null);
