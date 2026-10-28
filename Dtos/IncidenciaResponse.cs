using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using UTMDesk.Api.Models;

namespace UTMDesk.Api.Dtos;

public sealed record IncidenciaResponse(
    string? Id,
    string Codigo,
    string Titulo,
    string Descripcion,
    string Categoria,
    string Prioridad,
    string Estado,
    Reportante Reportante,
    Ubicacion Ubicacion,
    JsonElement DetalleEspecifico,
    TecnicoAsignado? TecnicoAsignado,
    IReadOnlyList<string> Tags,
    IReadOnlyList<EntradaHistorial> HistorialSeguimiento,
    IReadOnlyList<EvidenciaAdjunta> EvidenciasAdjuntas,
    double HorasInvertidas,
    DateTime FechaCreacion,
    DateTime FechaActualizacion,
    DateTime? FechaResolucion)
{
    public static IncidenciaResponse FromModel(
        Incidencia incidencia,
        bool incluirRegistrosInternos = false)
    {
        var detalleJson = incidencia.DetalleEspecifico.ToJson(
            new JsonWriterSettings { OutputMode = JsonOutputMode.RelaxedExtendedJson });
        var detalle = JsonDocument.Parse(detalleJson).RootElement.Clone();

        var historial = incluirRegistrosInternos
            ? incidencia.HistorialSeguimiento
            : incidencia.HistorialSeguimiento
                .Where(entrada => entrada.Visibilidad != "interna")
                .ToList();

        var evidencias = incluirRegistrosInternos
            ? incidencia.EvidenciasAdjuntas
            : incidencia.EvidenciasAdjuntas
                .Where(evidencia => evidencia.Visibilidad != "interna")
                .ToList();

        return new IncidenciaResponse(
            incidencia.Id,
            incidencia.Codigo,
            incidencia.Titulo,
            incidencia.Descripcion,
            incidencia.Categoria,
            incidencia.Prioridad,
            incidencia.Estado,
            incidencia.Reportante,
            incidencia.Ubicacion,
            detalle,
            incidencia.TecnicoAsignado,
            incidencia.Tags,
            historial,
            evidencias,
            incidencia.HorasInvertidas,
            incidencia.FechaCreacion,
            incidencia.FechaActualizacion,
            incidencia.FechaResolucion);
    }
}
