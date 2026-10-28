using System.ComponentModel.DataAnnotations;

namespace UTMDesk.Api.Dtos;

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password);

public sealed record UsuarioSesionResponse(
    string Id,
    string Nombre,
    string Email,
    string Rol,
    string? Area,
    string? Especialidad,
    string? TipoReportante);

public sealed record LoginResponse(
    string Token,
    DateTime ExpiraEn,
    UsuarioSesionResponse Usuario);
