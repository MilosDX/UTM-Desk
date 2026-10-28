using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using UTMDesk.Api.Constants;

namespace UTMDesk.Api.Dtos;

public sealed record UbicacionRequest(
    [Required, MaxLength(100)] string Edificio,
    [Required, MaxLength(100)] string Area);

public sealed record CrearIncidenciaRequest(
    [Required, MaxLength(150)] string Titulo,
    [Required, MaxLength(2000)] string Descripcion,
    [Required] string Categoria,
    [Required] string Prioridad,
    [Required] UbicacionRequest Ubicacion,
    JsonElement DetalleEspecifico,
    IReadOnlyList<string>? Tags);

public sealed record AsignarTecnicoRequest(
    [Required] string TecnicoId);

public sealed record RegistrarSeguimientoRequest(
    [Required] string EstadoNuevo,
    [Required, MinLength(10), MaxLength(2000)] string Comentario,
    [Range(0, 24)] double HorasTrabajadas,
    string Visibilidad = Visibilidades.Publica);

public sealed record CerrarIncidenciaRequest(
    [Required, MinLength(10), MaxLength(1000)] string Comentario);

public sealed record ReabrirIncidenciaRequest(
    [Required, MinLength(10), MaxLength(1000)] string Motivo);

public sealed class IncidenciasQuery
{
    public string? Estado { get; init; }
    public string? Categoria { get; init; }
    public string? Prioridad { get; init; }
    public string? Tag { get; init; }
    public string? Texto { get; init; }

    [Range(1, int.MaxValue)]
    public int Pagina { get; init; } = 1;

    [Range(1, 100)]
    public int TamanoPagina { get; init; } = 20;
}

public sealed class EvidenciaFormRequest
{
    [Required]
    public IFormFile Archivo { get; init; } = default!;

    [MaxLength(500)]
    public string? Descripcion { get; init; }

    public string Visibilidad { get; init; } = Visibilidades.Publica;
}
