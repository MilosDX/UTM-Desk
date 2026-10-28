using System.Security.Claims;

namespace UTMDesk.Api.Security;

public sealed record UsuarioActual(
    string Id,
    string Nombre,
    string Email,
    string Rol,
    string? Area,
    string? TipoReportante)
{
    public static UsuarioActual FromClaims(ClaimsPrincipal principal)
    {
        var id = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var nombre = principal.FindFirstValue(ClaimTypes.Name);
        var email = principal.FindFirstValue(ClaimTypes.Email);
        var rol = principal.FindFirstValue(ClaimTypes.Role);

        if (string.IsNullOrWhiteSpace(id) ||
            string.IsNullOrWhiteSpace(nombre) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(rol))
        {
            throw new UnauthorizedAccessException("El token no contiene los datos requeridos del usuario.");
        }

        return new UsuarioActual(
            id,
            nombre,
            email,
            rol,
            principal.FindFirstValue("area"),
            principal.FindFirstValue("tipo_reportante"));
    }
}
