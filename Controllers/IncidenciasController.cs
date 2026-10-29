using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTMDesk.Api.Constants;
using UTMDesk.Api.Dtos;
using UTMDesk.Api.Security;
using UTMDesk.Api.Services;

namespace UTMDesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/incidencias")]
public sealed class IncidenciasController : ControllerBase
{
    private readonly IIncidenciasService _incidenciasService;

    public IncidenciasController(IIncidenciasService incidenciasService)
    {
        _incidenciasService = incidenciasService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginaResponse<IncidenciaResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginaResponse<IncidenciaResponse>>> Obtener(
        [FromQuery] IncidenciasQuery query,
        CancellationToken cancellationToken)
    {
        var usuario = UsuarioActual.FromClaims(User);
        var pagina = await _incidenciasService.ObtenerAsync(
            query,
            usuario,
            cancellationToken);
        var incluirInternos = usuario.Rol != Roles.Reportante;

        return Ok(new PaginaResponse<IncidenciaResponse>(
            pagina.Elementos
                .Select(item => IncidenciaResponse.FromModel(item, incluirInternos))
                .ToList(),
            pagina.Total,
            pagina.Pagina,
            pagina.TamanoPagina));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(IncidenciaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidenciaResponse>> ObtenerPorId(
        string id,
        CancellationToken cancellationToken)
    {
        var usuario = UsuarioActual.FromClaims(User);
        var incidencia = await _incidenciasService.ObtenerPorIdAsync(
            id,
            usuario,
            cancellationToken);

        return incidencia is null
            ? NotFound()
            : Ok(IncidenciaResponse.FromModel(
                incidencia,
                usuario.Rol != Roles.Reportante));
    }

    [Authorize(Roles = Roles.Reportante + "," + Roles.SuperAdmin)]
    [HttpPost]
    [ProducesResponseType(typeof(IncidenciaResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<IncidenciaResponse>> Crear(
        CrearIncidenciaRequest request,
        CancellationToken cancellationToken)
    {
        var usuario = UsuarioActual.FromClaims(User);
        var incidencia = await _incidenciasService.CrearAsync(
            request,
            usuario,
            cancellationToken);
        var response = IncidenciaResponse.FromModel(incidencia, false);

        return CreatedAtAction(
            nameof(ObtenerPorId),
            new { id = incidencia.Id },
            response);
    }

    [Authorize(Roles = Roles.Coordinador + "," + Roles.SuperAdmin)]
    [HttpPatch("{id}/asignacion")]
    public async Task<ActionResult<IncidenciaResponse>> AsignarTecnico(
        string id,
        AsignarTecnicoRequest request,
        CancellationToken cancellationToken)
    {
        var incidencia = await _incidenciasService.AsignarTecnicoAsync(
            id,
            request.TecnicoId,
            UsuarioActual.FromClaims(User),
            cancellationToken);

        return Ok(IncidenciaResponse.FromModel(incidencia, true));
    }

    [Authorize(Roles = Roles.Tecnico + "," + Roles.SuperAdmin)]
    [HttpPost("{id}/seguimientos")]
    public async Task<ActionResult<IncidenciaResponse>> RegistrarSeguimiento(
        string id,
        RegistrarSeguimientoRequest request,
        CancellationToken cancellationToken)
    {
        var incidencia = await _incidenciasService.RegistrarSeguimientoAsync(
            id,
            request,
            UsuarioActual.FromClaims(User),
            cancellationToken);

        return Ok(IncidenciaResponse.FromModel(incidencia, true));
    }

    [Authorize(Roles = Roles.Coordinador + "," + Roles.SuperAdmin)]
    [HttpPatch("{id}/cerrar")]
    public async Task<ActionResult<IncidenciaResponse>> Cerrar(
        string id,
        CerrarIncidenciaRequest request,
        CancellationToken cancellationToken)
    {
        var incidencia = await _incidenciasService.CerrarAsync(
            id,
            request.Comentario,
            UsuarioActual.FromClaims(User),
            cancellationToken);

        return Ok(IncidenciaResponse.FromModel(incidencia, true));
    }

    [Authorize(Roles = Roles.Reportante + "," + Roles.Coordinador + "," + Roles.SuperAdmin)]
    [HttpPatch("{id}/reabrir")]
    public async Task<ActionResult<IncidenciaResponse>> Reabrir(
        string id,
        ReabrirIncidenciaRequest request,
        CancellationToken cancellationToken)
    {
        var usuario = UsuarioActual.FromClaims(User);
        var incidencia = await _incidenciasService.ReabrirAsync(
            id,
            request.Motivo,
            usuario,
            cancellationToken);

        return Ok(IncidenciaResponse.FromModel(
            incidencia,
            usuario.Rol != Roles.Reportante));
    }

    [Authorize(Roles = Roles.SuperAdmin)]
    [HttpPut("{id}")]
    public async Task<ActionResult<IncidenciaResponse>> Actualizar(
        string id,
        ActualizarIncidenciaRequest request,
        CancellationToken cancellationToken)
    {
        var incidencia = await _incidenciasService.ActualizarAsync(
            id,
            request,
            UsuarioActual.FromClaims(User),
            cancellationToken);

        return Ok(IncidenciaResponse.FromModel(incidencia, true));
    }

    [Authorize(Roles = Roles.SuperAdmin)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Eliminar(
        string id,
        CancellationToken cancellationToken)
    {
        var eliminada = await _incidenciasService.EliminarAsync(
            id,
            UsuarioActual.FromClaims(User),
            cancellationToken);

        return eliminada ? NoContent() : NotFound();
    }

    [Authorize(Roles = Roles.Reportante + "," + Roles.Tecnico + "," + Roles.Coordinador + "," + Roles.SuperAdmin)]
    [HttpPost("{id}/evidencias")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<EvidenciaResponse>> AgregarEvidencia(
        string id,
        [FromForm] EvidenciaFormRequest request,
        CancellationToken cancellationToken)
    {
        await using var contenido = request.Archivo.OpenReadStream();
        var evidencia = await _incidenciasService.AgregarEvidenciaAsync(
            id,
            contenido,
            request.Archivo.FileName,
            request.Archivo.ContentType,
            request.Archivo.Length,
            request.Descripcion,
            request.Visibilidad,
            UsuarioActual.FromClaims(User),
            cancellationToken);

        return Ok(evidencia);
    }

    [HttpGet("{id}/evidencias")]
    public async Task<ActionResult<IReadOnlyList<EvidenciaResponse>>> ObtenerEvidencias(
        string id,
        CancellationToken cancellationToken)
    {
        var evidencias = await _incidenciasService.ObtenerEvidenciasAsync(
            id,
            UsuarioActual.FromClaims(User),
            cancellationToken);

        return Ok(evidencias);
    }
}
