using UTMDesk.Api.Models;
using UTMDesk.Api.Dtos;
using UTMDesk.Api.Security;

namespace UTMDesk.Api.Services;

public interface IUsuariosService
{
    Task<Usuario?> ObtenerPorEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<Usuario?> ObtenerPorIdAsync(
        string id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Usuario>> ObtenerTecnicosAsync(
        string? especialidad,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Usuario>> ObtenerTodosAsync(
        CancellationToken cancellationToken = default);

    Task<Usuario> CrearAsync(
        CrearUsuarioRequest request,
        CancellationToken cancellationToken = default);

    Task<Usuario?> ActualizarAsync(
        string id,
        ActualizarUsuarioRequest request,
        UsuarioActual usuario,
        CancellationToken cancellationToken = default);

    Task<bool> EliminarAsync(
        string id,
        UsuarioActual usuario,
        CancellationToken cancellationToken = default);

    Task<Usuario?> CambiarEstadoAsync(
        string id,
        bool activo,
        CancellationToken cancellationToken = default);

    Task<bool> CambiarPasswordAsync(
        string id,
        string password,
        CancellationToken cancellationToken = default);

    Task CrearSuperAdminSiNoExisteAsync(
        CancellationToken cancellationToken = default);
}
