namespace UTMDesk.Api.Dtos;

public sealed record PaginaResponse<T>(
    IReadOnlyList<T> Elementos,
    long Total,
    int Pagina,
    int TamanoPagina);
