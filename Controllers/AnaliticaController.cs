using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTMDesk.Api.Constants;
using UTMDesk.Api.Dtos;
using UTMDesk.Api.Security;
using UTMDesk.Api.Services;

namespace UTMDesk.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Coordinador + "," + Roles.Directivo + "," + Roles.SuperAdmin)]
[Route("api/analitica")]
public sealed class AnaliticaController : ControllerBase
{
    private readonly IIncidenciasService _incidenciasService;

    public AnaliticaController(IIncidenciasService incidenciasService)
    {
        _incidenciasService = incidenciasService;
    }

    [HttpGet("resumen")]
    public async Task<ActionResult<AnaliticaResponse>> ObtenerResumen(
        CancellationToken cancellationToken)
    {
        var resultado = await _incidenciasService.ObtenerAnaliticaAsync(
            UsuarioActual.FromClaims(User),
            cancellationToken);

        return Ok(resultado);
    }
}
