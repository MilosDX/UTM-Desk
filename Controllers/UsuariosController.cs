using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTMDesk.Api.Constants;
using UTMDesk.Api.Dtos;
using UTMDesk.Api.Security;
using UTMDesk.Api.Services;

namespace UTMDesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/usuarios")]
public sealed class UsuariosController : ControllerBase
{
    private readonly IUsuariosService _usuariosService;

    public UsuariosController(IUsuariosService usuariosService)
    {
        _usuariosService = usuariosService;
    }

    [HttpGet("tecnicos")]
    [Authorize(Roles = $"{Roles.Coordinador},{Roles.SuperAdmin}")]
    public async Task<ActionResult<IReadOnlyList<UsuarioResponse>>> ObtenerTecnicos(
        [FromQuery] string? especialidad,
        CancellationToken cancellationToken)
    {
        var tecnicos = await _usuariosService.ObtenerTecnicosAsync(
            especialidad,
            cancellationToken);

        return Ok(tecnicos.Select(usuario => new UsuarioResponse(
            usuario.Id!,
            usuario.Nombre,
            usuario.Email,
            usuario.Rol,
            usuario.TipoReportante,
            usuario.Area,
            usuario.Especialidad,
            usuario.Activo)));
    }

    [HttpGet]
    [Authorize(Roles = Roles.SuperAdmin)]
    public async Task<ActionResult<IReadOnlyList<UsuarioResponse>>> ObtenerTodos(
        CancellationToken cancellationToken)
    {
        var usuarios = await _usuariosService.ObtenerTodosAsync(cancellationToken);
        return Ok(usuarios.Select(Mapear));
    }

    [HttpPost]
    [Authorize(Roles = Roles.SuperAdmin)]
    public async Task<ActionResult<UsuarioResponse>> Crear(
        CrearUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        var usuario = await _usuariosService.CrearAsync(request, cancellationToken);
        return CreatedAtAction(nameof(ObtenerTodos), Mapear(usuario));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = Roles.SuperAdmin)]
    public async Task<ActionResult<UsuarioResponse>> Actualizar(
        string id,
        ActualizarUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        var usuario = await _usuariosService.ActualizarAsync(
            id,
            request,
            UsuarioActual.FromClaims(User),
            cancellationToken);

        return usuario is null ? NotFound() : Ok(Mapear(usuario));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.SuperAdmin)]
    public async Task<IActionResult> Eliminar(
        string id,
        CancellationToken cancellationToken)
    {
        var eliminado = await _usuariosService.EliminarAsync(
            id,
            UsuarioActual.FromClaims(User),
            cancellationToken);

        return eliminado ? NoContent() : NotFound();
    }

    [HttpPatch("{id}/estado")]
    [Authorize(Roles = Roles.SuperAdmin)]
    public async Task<ActionResult<UsuarioResponse>> CambiarEstado(
        string id,
        CambiarEstadoUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        var usuario = await _usuariosService.CambiarEstadoAsync(
            id,
            request.Activo,
            cancellationToken);

        return usuario is null ? NotFound() : Ok(Mapear(usuario));
    }

    [HttpPatch("{id}/password")]
    [Authorize(Roles = Roles.SuperAdmin)]
    public async Task<IActionResult> CambiarPassword(
        string id,
        CambiarPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var actualizado = await _usuariosService.CambiarPasswordAsync(
            id,
            request.Password,
            cancellationToken);

        return actualizado ? NoContent() : NotFound();
    }

    private static UsuarioResponse Mapear(Models.Usuario usuario) =>
        new(
            usuario.Id!,
            usuario.Nombre,
            usuario.Email,
            usuario.Rol,
            usuario.TipoReportante,
            usuario.Area,
            usuario.Especialidad,
            usuario.Activo);
}
