using UTMDesk.Api.Dtos;
using UTMDesk.Api.Models;

namespace UTMDesk.Api.Services;

public interface IAuthService
{
    Task<LoginResponse?> IniciarSesionAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);

    Task<Usuario?> ObtenerUsuarioAsync(
        string id,
        CancellationToken cancellationToken = default);
}
