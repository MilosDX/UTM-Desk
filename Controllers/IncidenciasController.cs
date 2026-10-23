using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using UTMDesk.Api.Dtos;
using UTMDesk.Api.Services;

namespace UTMDesk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class IncidenciasController : ControllerBase
{
    private readonly IIncidenciasService _incidenciasService;

    public IncidenciasController(IIncidenciasService incidenciasService)
    {
        _incidenciasService = incidenciasService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<IncidenciaResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<IReadOnlyList<IncidenciaResponse>>> ObtenerTodas(
        CancellationToken cancellationToken)
    {
        try
        {
            var incidencias = await _incidenciasService.ObtenerTodasAsync(cancellationToken);
            return Ok(incidencias.Select(IncidenciaResponse.FromModel).ToList());
        }
        catch (InvalidOperationException exception)
        {
            return Problem(
                title: "MongoDB Atlas no está configurado",
                detail: exception.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (MongoException)
        {
            return Problem(
                title: "No se pudo conectar con MongoDB Atlas",
                detail: "Revisa la cadena de conexión y que tu IP esté permitida en Network Access.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}
