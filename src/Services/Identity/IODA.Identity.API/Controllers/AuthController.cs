using IODA.Identity.Application.Commands;
using IODA.Identity.Application.DTOs;
using IODA.Identity.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IODA.Identity.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // -----------------------------------------------------------------------
    // Endpoints públicos (login, registro, refresh, setup status)
    // -----------------------------------------------------------------------

    /// <summary>Estado de configuración del sistema: si existen usuarios y si el auto-registro está habilitado.</summary>
    [HttpGet("setup-status")]
    [ProducesResponseType(typeof(SetupStatusDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SetupStatusDto>> GetSetupStatus(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSetupStatusQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>Registrar un nuevo usuario.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RegisterResultDto>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var command = new RegisterCommand(request.Email, request.Password, request.DisplayName);
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(Register), new { id = result.UserId }, result);
    }

    /// <summary>Iniciar sesión (email + contraseña). Devuelve access token y refresh token.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResultDto>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var command = new LoginCommand(request.Email, request.Password);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>Obtener un nuevo access token usando un refresh token válido.</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(LoginResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResultDto>> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var command = new RefreshTokenCommand(request.RefreshToken);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    // -----------------------------------------------------------------------
    // Endpoints de administración (requieren JWT)
    // -----------------------------------------------------------------------

    /// <summary>Listar todos los usuarios (admin).</summary>
    [HttpGet("users")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<UserListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<UserListItemDto>>> ListUsers(CancellationToken cancellationToken)
    {
        var query = new ListUsersQuery();
        var list = await _mediator.Send(query, cancellationToken);
        return Ok(list ?? Array.Empty<UserListItemDto>());
    }
}

public record RegisterRequest(string Email, string Password, string? DisplayName = null);
public record LoginRequest(string Email, string Password);
public record RefreshTokenRequest(string RefreshToken);
