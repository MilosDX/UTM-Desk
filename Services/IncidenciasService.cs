using Microsoft.Extensions.Options;
using MongoDB.Driver;
using UTMDesk.Api.Configuration;
using UTMDesk.Api.Models;

namespace UTMDesk.Api.Services;

public sealed class IncidenciasService : IIncidenciasService
{
    private readonly IMongoCollection<Incidencia>? _incidencias;

    public IncidenciasService(IOptions<MongoDbSettings> options)
    {
        var settings = options.Value;

        if (string.IsNullOrWhiteSpace(settings.ConnectionString))
        {
            return;
        }

        var cliente = new MongoClient(settings.ConnectionString);
        var baseDeDatos = cliente.GetDatabase(settings.DatabaseName);
        _incidencias = baseDeDatos.GetCollection<Incidencia>(
            settings.IncidenciasCollectionName);
    }

    public async Task<IReadOnlyList<Incidencia>> ObtenerTodasAsync(
        CancellationToken cancellationToken = default)
    {
        if (_incidencias is null)
        {
            throw new InvalidOperationException(
                "Falta configurar MongoDb:ConnectionString en appsettings.Local.json.");
        }

        return await _incidencias
            .Find(Builders<Incidencia>.Filter.Empty)
            .SortByDescending(incidencia => incidencia.FechaCreacion)
            .ToListAsync(cancellationToken);
    }
}
