namespace UTMDesk.Api.Services;

public interface IEvidenciaStorageService
{
    bool EstaConfigurado { get; }

    Task<string> SubirAsync(
        Stream contenido,
        string nombreArchivo,
        string tipoMime,
        string incidenciaId,
        CancellationToken cancellationToken = default);

    Task<string> CrearUrlTemporalAsync(
        string rutaStorage,
        CancellationToken cancellationToken = default);
}
