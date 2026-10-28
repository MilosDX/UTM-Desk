namespace UTMDesk.Api.Constants;

public static class Roles
{
    public const string Reportante = "reportante";
    public const string Tecnico = "tecnico";
    public const string Coordinador = "coordinador";
    public const string Directivo = "directivo";
    public const string SuperAdmin = "superAdmin";

    public static readonly IReadOnlySet<string> Todos =
        new HashSet<string>(StringComparer.Ordinal)
        {
            Reportante,
            Tecnico,
            Coordinador,
            Directivo,
            SuperAdmin
        };
}
