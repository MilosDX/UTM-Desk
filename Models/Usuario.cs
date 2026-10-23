using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UTMDesk.Api.Models;

[BsonIgnoreExtraElements]
public sealed class Usuario
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("password_hash")]
    [BsonIgnoreIfNull]
    public string? PasswordHash { get; set; }

    [BsonElement("rol")]
    public string Rol { get; set; } = string.Empty;

    [BsonElement("tipo_reportante")]
    [BsonIgnoreIfNull]
    public string? TipoReportante { get; set; }

    [BsonElement("area")]
    [BsonIgnoreIfNull]
    public string? Area { get; set; }

    [BsonElement("especialidad")]
    [BsonIgnoreIfNull]
    public string? Especialidad { get; set; }

    [BsonElement("activo")]
    public bool Activo { get; set; }

    [BsonElement("fecha_creacion")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime FechaCreacion { get; set; }
}
