using System.ComponentModel.DataAnnotations;

namespace UTMDesk.Api.Dtos;

public sealed record CrearUsuarioRequest(
    [Required, MaxLength(120)] string Nombre,
    [Required, EmailAddress, MaxLength(200)] string Email,
    [Required, MinLength(8), MaxLength(100)] string Password,
    [Required] string Rol,
    string? TipoReportante,
    [MaxLength(120)] string? Area,
    [MaxLength(120)] string? Especialidad);

public sealed record CambiarEstadoUsuarioRequest(bool Activo);

public sealed record CambiarPasswordRequest(
    [Required, MinLength(8), MaxLength(100)] string Password);
