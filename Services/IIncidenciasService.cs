using UTMDesk.Api.Models;

namespace UTMDesk.Api.Services;

public interface IIncidenciasService
{
    Task<IReadOnlyList<Incidencia>> ObtenerTodasAsync(
        CancellationToken cancellationToken = default);
}
