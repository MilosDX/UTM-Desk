namespace UTMDesk.Api.Constants;

public static class Prioridades
{
    public const string Baja = "baja";
    public const string Media = "media";
    public const string Alta = "alta";
    public const string Urgente = "urgente";

    public static readonly IReadOnlySet<string> Todas = new HashSet<string>(
        [Baja, Media, Alta, Urgente],
        StringComparer.OrdinalIgnoreCase);
}
