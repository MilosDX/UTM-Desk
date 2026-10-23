namespace UTMDesk.Api.Configuration;

public sealed class MongoDbSettings
{
    public const string SectionName = "MongoDb";

    public string ConnectionString { get; init; } = string.Empty;
    public string DatabaseName { get; init; } = "utm_desk_cloud";
    public string UsuariosCollectionName { get; init; } = "usuarios";
    public string IncidenciasCollectionName { get; init; } = "incidencias_campus";
}
