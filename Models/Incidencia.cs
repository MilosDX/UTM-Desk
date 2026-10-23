using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UTMDesk.Api.Models;

[BsonIgnoreExtraElements]
public sealed class Incidencia
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("codigo")]
    public string Codigo { get; set; } = string.Empty;

    [BsonElement("titulo")]
    public string Titulo { get; set; } = string.Empty;

    [BsonElement("descripcion")]
    public string Descripcion { get; set; } = string.Empty;

    [BsonElement("categoria")]
    public string Categoria { get; set; } = string.Empty;

    [BsonElement("prioridad")]
    public string Prioridad { get; set; } = string.Empty;

    [BsonElement("estado")]
    public string Estado { get; set; } = string.Empty;

    [BsonElement("reportante")]
    public Reportante Reportante { get; set; } = new();

    [BsonElement("ubicacion")]
    public Ubicacion Ubicacion { get; set; } = new();

    // Es polimórfico: cada categoría puede guardar campos diferentes.
    [BsonElement("detalle_especifico")]
    public BsonDocument DetalleEspecifico { get; set; } = new();

    [BsonElement("tecnico_asignado")]
    [BsonIgnoreIfNull]
    public TecnicoAsignado? TecnicoAsignado { get; set; }

    [BsonElement("tags")]
    public List<string> Tags { get; set; } = [];

    [BsonElement("historial_seguimiento")]
    public List<EntradaHistorial> HistorialSeguimiento { get; set; } = [];

    [BsonElement("evidencias_adjuntas")]
    public List<EvidenciaAdjunta> EvidenciasAdjuntas { get; set; } = [];

    [BsonElement("horas_invertidas")]
    public double HorasInvertidas { get; set; }

    [BsonElement("fecha_creacion")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime FechaCreacion { get; set; }

    [BsonElement("fecha_actualizacion")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime FechaActualizacion { get; set; }

    [BsonElement("fecha_resolucion")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [BsonIgnoreIfNull]
    public DateTime? FechaResolucion { get; set; }
}

[BsonIgnoreExtraElements]
public sealed class Reportante
{
    [BsonElement("usuario_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? UsuarioId { get; set; }

    [BsonElement("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [BsonElement("tipo")]
    public string Tipo { get; set; } = string.Empty;
}

[BsonIgnoreExtraElements]
public sealed class Ubicacion
{
    [BsonElement("edificio")]
    public string Edificio { get; set; } = string.Empty;

    [BsonElement("area")]
    public string Area { get; set; } = string.Empty;
}

[BsonIgnoreExtraElements]
public sealed class TecnicoAsignado
{
    [BsonElement("usuario_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? UsuarioId { get; set; }

    [BsonElement("nombre")]
    public string Nombre { get; set; } = string.Empty;
}

[BsonIgnoreExtraElements]
public sealed class EntradaHistorial
{
    [BsonElement("accion")]
    public string Accion { get; set; } = string.Empty;

    [BsonElement("estado_nuevo")]
    [BsonIgnoreIfNull]
    public string? EstadoNuevo { get; set; }

    [BsonElement("comentario")]
    [BsonIgnoreIfNull]
    public string? Comentario { get; set; }

    [BsonElement("usuario_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? UsuarioId { get; set; }

    [BsonElement("horas_trabajadas")]
    [BsonIgnoreIfNull]
    public double? HorasTrabajadas { get; set; }

    [BsonElement("fecha")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime Fecha { get; set; }
}

[BsonIgnoreExtraElements]
public sealed class EvidenciaAdjunta
{
    [BsonElement("nombre_archivo")]
    public string NombreArchivo { get; set; } = string.Empty;

    [BsonElement("tipo_mime")]
    public string TipoMime { get; set; } = string.Empty;

    [BsonElement("tamano_bytes")]
    public long TamanoBytes { get; set; }

    [BsonElement("ruta_storage")]
    public string RutaStorage { get; set; } = string.Empty;

    [BsonElement("fecha_carga")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime FechaCarga { get; set; }
}
