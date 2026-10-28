namespace UTMDesk.Api.Constants;

public static class CategoriasIncidencia
{
    public const string ComputoRedes = "computo_redes";
    public const string Infraestructura = "infraestructura";
    public const string ServiciosAcademicos = "academico";

    public static readonly IReadOnlySet<string> Todas = new HashSet<string>(
        [ComputoRedes, Infraestructura, ServiciosAcademicos],
        StringComparer.OrdinalIgnoreCase);
}
