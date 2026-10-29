using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using UTMDesk.Api.Configuration;

namespace UTMDesk.Api.Services;

public sealed class SupabaseEvidenciaStorageService : IEvidenciaStorageService
{
    private readonly HttpClient _httpClient;
    private readonly SupabaseSettings _settings;

    public SupabaseEvidenciaStorageService(
        HttpClient httpClient,
        IOptions<SupabaseSettings> options)
    {
        _httpClient = httpClient;
        _settings = options.Value;
    }

    public bool EstaConfigurado => _settings.IsConfigured;

    public async Task<string> SubirAsync(
        Stream contenido,
        string nombreArchivo,
        string tipoMime,
        string incidenciaId,
        CancellationToken cancellationToken = default)
    {
        ValidarConfiguracion();

        var extension = Path.GetExtension(nombreArchivo).ToLowerInvariant();
        var ruta = $"incidencias/{incidenciaId}/{Guid.NewGuid():N}{extension}";
        var url = ConstruirUrl($"storage/v1/object/{_settings.BucketName}/{ruta}");

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        AgregarAutorizacion(request);
        request.Headers.TryAddWithoutValidation("x-upsert", "false");

        request.Content = new StreamContent(contenido);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(tipoMime);

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var detalle = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Supabase rechazó la evidencia ({(int)response.StatusCode}): {detalle}");
        }

        return ruta;
    }

    public async Task<string> CrearUrlTemporalAsync(
        string rutaStorage,
        CancellationToken cancellationToken = default)
    {
        ValidarConfiguracion();

        var url = ConstruirUrl(
            $"storage/v1/object/sign/{_settings.BucketName}/{rutaStorage}");

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        AgregarAutorizacion(request);
        request.Content = JsonContent.Create(new
        {
            expiresIn = Math.Max(60, _settings.SignedUrlExpirationMinutes * 60)
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var contenido = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"No se pudo firmar la evidencia ({(int)response.StatusCode}): {contenido}");
        }

        using var json = JsonDocument.Parse(contenido);
        var raiz = json.RootElement;
        var rutaFirmada = raiz.TryGetProperty("signedURL", out var signedUrl)
            ? signedUrl.GetString()
            : raiz.TryGetProperty("signedUrl", out var signedUrlAlterna)
                ? signedUrlAlterna.GetString()
                : null;

        if (string.IsNullOrWhiteSpace(rutaFirmada))
        {
            throw new InvalidOperationException("Supabase no devolvió una URL firmada.");
        }

        if (rutaFirmada.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return rutaFirmada;
        }

        // Supabase devuelve la ruta firmada relativa a /storage/v1, por ejemplo
        // "/object/sign/bucket/ruta?token=..." Hay que anteponer "storage/v1".
        var rutaRelativa = rutaFirmada.TrimStart('/');
        if (!rutaRelativa.StartsWith("storage/v1/", StringComparison.OrdinalIgnoreCase))
        {
            rutaRelativa = $"storage/v1/{rutaRelativa}";
        }

        return $"{_settings.Url.TrimEnd('/')}/{rutaRelativa}";
    }

    private void ValidarConfiguracion()
    {
        if (!EstaConfigurado)
        {
            throw new InvalidOperationException(
                "Falta configurar Supabase:Url, Supabase:SecretKey y el bucket de evidencias.");
        }
    }

    private string ConstruirUrl(string ruta) =>
        $"{_settings.Url.TrimEnd('/')}/{ruta}";

    private void AgregarAutorizacion(HttpRequestMessage request)
    {
        request.Headers.TryAddWithoutValidation("apikey", _settings.ApiKey);

        if (!_settings.ApiKey.StartsWith("sb_secret_", StringComparison.Ordinal))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                _settings.ApiKey);
        }
    }
}
