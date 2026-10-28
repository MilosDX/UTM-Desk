using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using UTMDesk.Api.Configuration;
using UTMDesk.Api.Dtos;
using UTMDesk.Api.Models;

namespace UTMDesk.Api.Services;

public sealed class TokenService : ITokenService
{
    private readonly JwtSettings _settings;

    public TokenService(IOptions<JwtSettings> options)
    {
        _settings = options.Value;
    }

    public LoginResponse CrearToken(Usuario usuario)
    {
        if (string.IsNullOrWhiteSpace(usuario.Id))
        {
            throw new InvalidOperationException("El usuario no tiene un identificador válido.");
        }

        if (string.IsNullOrWhiteSpace(_settings.Key) || _settings.Key.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:Key debe contener al menos 32 caracteres.");
        }

        var expiraEn = DateTime.UtcNow.AddMinutes(_settings.ExpirationMinutes);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id),
            new(ClaimTypes.Name, usuario.Nombre),
            new(ClaimTypes.Email, usuario.Email),
            new(ClaimTypes.Role, usuario.Rol),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (!string.IsNullOrWhiteSpace(usuario.Area))
        {
            claims.Add(new Claim("area", usuario.Area));
        }

        if (!string.IsNullOrWhiteSpace(usuario.TipoReportante))
        {
            claims.Add(new Claim("tipo_reportante", usuario.TipoReportante));
        }

        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiraEn,
            signingCredentials: credenciales);

        return new LoginResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiraEn,
            new UsuarioSesionResponse(
                usuario.Id,
                usuario.Nombre,
                usuario.Email,
                usuario.Rol,
                usuario.Area,
                usuario.Especialidad,
                usuario.TipoReportante));
    }
}
