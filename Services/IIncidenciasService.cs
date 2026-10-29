using UTMDesk.Api.Dtos;
using UTMDesk.Api.Models;
using UTMDesk.Api.Security;

namespace UTMDesk.Api.Services;

public interface IIncidenciasService
{
    Task<PaginaResponse<Incidencia>> ObtenerAsync(
        IncidenciasQuery query,
        UsuarioActual usuario,
        CancellationToken cancellationToken = default);

    Task<Incidencia?> ObtenerPorIdAsync(
        string id,
        UsuarioActual usuario,
        CancellationToken cancellationToken = default);

    Task<Incidencia> CrearAsync(
        CrearIncidenciaRequest request,
        UsuarioActual usuario,
        CancellationToken cancellationToken = default);

    Task<Incidencia> AsignarTecnicoAsync(
        string id,
        string tecnicoId,
        UsuarioActual usuario,
        CancellationToken cancellationToken = default);

    Task<Incidencia> ActualizarAsync(
        string id,
        ActualizarIncidenciaRequest request,
        UsuarioActual usuario,
        CancellationToken cancellationToken = default);

    Task<bool> EliminarAsync(
        string id,
        UsuarioActual usuario,
        CancellationToken cancellationToken = default);

    Task<Incidencia> RegistrarSeguimientoAsync(
        string id,
        RegistrarSeguimientoRequest request,
        UsuarioActual usuario,
        CancellationToken cancellationToken = default);

    Task<Incidencia> CerrarAsync(
        string id,
        string comentario,
        UsuarioActual usuario,
        CancellationToken cancellationToken = default);

    Task<Incidencia> ReabrirAsync(
        string id,
        string motivo,
        UsuarioActual usuario,
        CancellationToken cancellationToken = default);

    Task<EvidenciaResponse> AgregarEvidenciaAsync(
        string id,
        Stream contenido,
        string nombreArchivo,
        string tipoMime,
        long tamanoBytes,
        string? descripcion,
        string visibilidad,
        UsuarioActual usuario,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EvidenciaResponse>> ObtenerEvidenciasAsync(
        string id,
        UsuarioActual usuario,
        CancellationToken cancellationToken = default);

    Task<AnaliticaResponse> ObtenerAnaliticaAsync(
        UsuarioActual usuario,
        CancellationToken cancellationToken = default);
}
