namespace UTMDesk.Api.Constants;

public static class EstadosIncidencia
{
    public const string Abierta = "abierta";
    public const string EnProceso = "en proceso";
    public const string EnEspera = "en espera";
    public const string Resuelta = "resuelta";
    public const string Cerrada = "cerrada";

    public static readonly IReadOnlySet<string> Todos = new HashSet<string>(
        [Abierta, EnProceso, EnEspera, Resuelta, Cerrada],
        StringComparer.OrdinalIgnoreCase);
}
