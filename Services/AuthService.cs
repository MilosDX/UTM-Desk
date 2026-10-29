using Microsoft.AspNetCore.Identity;
using UTMDesk.Api.Dtos;
using UTMDesk.Api.Models;

namespace UTMDesk.Api.Services;

public sealed class AuthService : IAuthService
{
    private readonly IUsuariosService _usuariosService;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher<Usuario> _passwordHasher;

    public AuthService(
        IUsuariosService usuariosService,
        ITokenService tokenService,
        IPasswordHasher<Usuario> passwordHasher)
    {
        _usuariosService = usuariosService;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
    }

    public async Task<LoginResponse?> IniciarSesionAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var usuario = await _usuariosService.ObtenerPorEmailAsync(
            request.Email,
            cancellationToken);

        if (usuario is null ||
            !usuario.Activo ||
            string.IsNullOrWhiteSpace(usuario.PasswordHash))
        {
            return null;
        }

        var resultado = VerificarPassword(
            usuario,
            usuario.PasswordHash,
            request.Password);

        return resultado
            ? _tokenService.CrearToken(usuario)
            : null;
    }

    private bool VerificarPassword(
        Usuario usuario,
        string hash,
        string password)
    {
        // Las cuentas históricas/demo usan bcrypt; las nuevas usan el
        // hasher de ASP.NET Identity (PBKDF2 en Base64).
        if (hash.StartsWith("$2", StringComparison.Ordinal))
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                return false;
            }
        }

        try
        {
            return _passwordHasher.VerifyHashedPassword(
                usuario,
                hash,
                password) != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public Task<Usuario?> ObtenerUsuarioAsync(
        string id,
        CancellationToken cancellationToken = default) =>
        _usuariosService.ObtenerPorIdAsync(id, cancellationToken);
}
