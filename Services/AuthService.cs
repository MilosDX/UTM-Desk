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

        var resultado = _passwordHasher.VerifyHashedPassword(
            usuario,
            usuario.PasswordHash,
            request.Password);

        return resultado == PasswordVerificationResult.Failed
            ? null
            : _tokenService.CrearToken(usuario);
    }

    public Task<Usuario?> ObtenerUsuarioAsync(
        string id,
        CancellationToken cancellationToken = default) =>
        _usuariosService.ObtenerPorIdAsync(id, cancellationToken);
}
