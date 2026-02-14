using IODA.Authorization.API.Contracts;
using IODA.Authorization.Application.Commands;
using IODA.Authorization.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IODA.Authorization.API.Controllers;

/// <summary>
/// API de autorización: permisos, roles y reglas de acceso.
/// Todos los endpoints requieren autenticación JWT. CRUD de roles/permisos/reglas requiere rol Admin.
/// El endpoint effective-permissions admite además API key de servicio (X-Service-Api-Key) para llamadas desde Identity.
/// </summary>
[ApiController]
[Route("api/authorization")]
[Authorize]
public class AuthorizationController : ControllerBase
{
    public const string ServiceApiKeyHeaderName = "X-Service-Api-Key";

    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;

    public AuthorizationController(IMediator mediator, IConfiguration configuration)
    {
        _mediator = mediator;
        _configuration = configuration;
    }

    private bool AllowEffectivePermissionsAccess()
    {
        var apiKey = _configuration["Authorization:ServiceApiKey"];
        if (!string.IsNullOrWhiteSpace(apiKey) && Request.Headers.TryGetValue(ServiceApiKeyHeaderName, out var headerKey) && headerKey == apiKey)
            return true;
        return User.Identity?.IsAuthenticated == true;
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

    /// <summary>
    /// Obtener códigos de permiso efectivos de un usuario (unión de permisos de todos sus roles).
    /// Admite autenticación JWT o API key de servicio (header X-Service-Api-Key) para llamadas desde Identity.
    /// </summary>
    [HttpGet("users/{userId:guid}/effective-permissions")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<string>>> GetEffectivePermissions(Guid userId, CancellationToken cancellationToken)
    {
        if (!AllowEffectivePermissionsAccess())
            return Unauthorized();

        var codes = await _mediator.Send(new GetEffectivePermissionsQuery(userId), cancellationToken);
        return Ok(codes);
    }

    /// <summary>
    /// 2.5: Asigna el rol SuperAdmin al primer usuario. Solo permitido cuando aún no existe ninguna regla de acceso.
    /// Identity llama a este endpoint tras registrar al primer usuario. Requiere API key de servicio o JWT.
    /// </summary>
    [HttpPost("bootstrap-first-user")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> BootstrapFirstUser([FromBody] BootstrapFirstUserRequest request, CancellationToken cancellationToken)
    {
        if (!AllowEffectivePermissionsAccess())
            return Unauthorized();

        var result = await _mediator.Send(new BootstrapFirstUserCommand(request.UserId), cancellationToken);
        if (!result.Success)
            return Conflict(new { error = result.ErrorMessage });
        return StatusCode(StatusCodes.Status201Created);
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
