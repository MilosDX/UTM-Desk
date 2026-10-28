namespace UTMDesk.Api.Constants;

public static class TiposReportante
{
    public const string Estudiante = "estudiante";
    public const string Docente = "docente";
    public const string Administrativo = "administrativo";

    public static readonly IReadOnlySet<string> Todos =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Estudiante,
            Docente,
            Administrativo
        };
}
