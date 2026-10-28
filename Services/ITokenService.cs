using UTMDesk.Api.Dtos;
using UTMDesk.Api.Models;

namespace UTMDesk.Api.Services;

public interface ITokenService
{
    LoginResponse CrearToken(Usuario usuario);
}
