namespace UTMDesk.Api.Dtos;

public sealed record UsuarioResponse(
    string Id,
    string Nombre,
    string Email,
    string Rol,
    string? TipoReportante,
    string? Area,
    string? Especialidad,
    bool Activo);
