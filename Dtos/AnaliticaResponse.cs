namespace UTMDesk.Api.Dtos;

public sealed record ConteoAgrupadoResponse(string Nombre, int Total);

public sealed record AnaliticaResponse(
    int TotalIncidencias,
    double HorasInvertidas,
    IReadOnlyList<ConteoAgrupadoResponse> PorEstado,
    IReadOnlyList<ConteoAgrupadoResponse> PorCategoria,
    IReadOnlyList<ConteoAgrupadoResponse> PorEdificio,
    IReadOnlyList<ConteoAgrupadoResponse> TagsFrecuentes);

public sealed record EvidenciaResponse(
    string NombreArchivo,
    string TipoMime,
    long TamanoBytes,
    string? Descripcion,
    string? NombreUsuario,
    DateTime FechaCarga,
    string UrlTemporal);
