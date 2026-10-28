using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTMDesk.Api.Dtos;
using UTMDesk.Api.Security;
using UTMDesk.Api.Services;

namespace UTMDesk.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var resultado = await _authService.IniciarSesionAsync(
            request,
            cancellationToken);

        return resultado is null
            ? Unauthorized(new { mensaje = "Correo o contraseña incorrectos." })
            : Ok(resultado);
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UsuarioSesionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UsuarioSesionResponse>> Me(
        CancellationToken cancellationToken)
    {
        var actual = UsuarioActual.FromClaims(User);
        var usuario = await _authService.ObtenerUsuarioAsync(
            actual.Id,
            cancellationToken);

        if (usuario is null)
        {
            return NotFound();
        }

        return Ok(new UsuarioSesionResponse(
            usuario.Id!,
            usuario.Nombre,
            usuario.Email,
            usuario.Rol,
            usuario.Area,
            usuario.Especialidad,
            usuario.TipoReportante));
    }
}
