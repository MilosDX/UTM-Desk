using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using UTMDesk.Api.Configuration;
using UTMDesk.Api.Constants;
using UTMDesk.Api.Dtos;
using UTMDesk.Api.Exceptions;
using UTMDesk.Api.Models;
using UTMDesk.Api.Security;

namespace UTMDesk.Api.Services;

public sealed class IncidenciasService : IIncidenciasService
{
    private const long TamanoMaximoEvidencia = 5 * 1024 * 1024;

    private static readonly IReadOnlySet<string> TiposEvidenciaPermitidos =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "application/pdf"
        };

    private static readonly IReadOnlySet<string> EstadosPermitidosTecnico =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            EstadosIncidencia.EnProceso,
            EstadosIncidencia.EnEspera,
            EstadosIncidencia.Resuelta
        };

    private readonly IMongoCollection<Incidencia>? _incidencias;
    private readonly IUsuariosService _usuariosService;
    private readonly IEvidenciaStorageService _storageService;

    public IncidenciasService(
        IOptions<MongoDbSettings> options,
        IUsuariosService usuariosService,
        IEvidenciaStorageService storageService)
    {
        _usuariosService = usuariosService;
        _storageService = storageService;

        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ConnectionString))
        {
            return;
        }

        var cliente = new MongoClient(settings.ConnectionString);
        _incidencias = cliente
            .GetDatabase(settings.DatabaseName)
            .GetCollection<Incidencia>(settings.IncidenciasCollectionName);
    }

    public async Task<PaginaResponse<Incidencia>> ObtenerAsync(
        IncidenciasQuery query,
        UsuarioActual usuario,
        CancellationToken cancellationToken = default)
    {
        var filtros = new List<FilterDefinition<Incidencia>>
        {
            ConstruirFiltroAcceso(usuario)
        };

        if (!string.IsNullOrWhiteSpace(query.Estado))
        {
            filtros.Add(Builders<Incidencia>.Filter.Eq(
                incidencia => incidencia.Estado,
                query.Estado.Trim().ToLowerInvariant()));
        }

        if (!string.IsNullOrWhiteSpace(query.Categoria))
        {
            filtros.Add(Builders<Incidencia>.Filter.Eq(
                incidencia => incidencia.Categoria,
                query.Categoria.Trim().ToLowerInvariant()));
        }

        if (!string.IsNullOrWhiteSpace(query.Prioridad))
        {
            filtros.Add(Builders<Incidencia>.Filter.Eq(
                incidencia => incidencia.Prioridad,
                query.Prioridad.Trim().ToLowerInvariant()));
        }

        if (!string.IsNullOrWhiteSpace(query.Tag))
        {
            filtros.Add(Builders<Incidencia>.Filter.AnyEq(
                incidencia => incidencia.Tags,
                query.Tag.Trim().TrimStart('#').ToLowerInvariant()));
        }

        if (!string.IsNullOrWhiteSpace(query.Texto))
        {
            filtros.Add(Builders<Incidencia>.Filter.Text(query.Texto.Trim()));
        }

        var filtro = Builders<Incidencia>.Filter.And(filtros);
        var coleccion = ObtenerColeccion();
        var total = await coleccion.CountDocumentsAsync(
            filtro,
            cancellationToken: cancellationToken);

        var elementos = await coleccion
            .Find(filtro)
            .SortByDescending(incidencia => incidencia.FechaCreacion)
            .Skip((query.Pagina - 1) * query.TamanoPagina)
            .Limit(query.TamanoPagina)
            .ToListAsync(cancellationToken);

        return new PaginaResponse<Incidencia>(
            elementos,
            total,
            query.Pagina,
            query.TamanoPagina);
    }

    public async Task<Incidencia?> ObtenerPorIdAsync(
        string id,
        UsuarioActual usuario,
        CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return null;
        }

        var filtro = Builders<Incidencia>.Filter.And(
            Builders<Incidencia>.Filter.Eq(incidencia => incidencia.Id, id),
            ConstruirFiltroAcceso(usuario));

        return await ObtenerColeccion()
            .Find(filtro)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Incidencia> CrearAsync(
        CrearIncidenciaRequest request,
        UsuarioActual usuario,
        CancellationToken cancellationToken = default)
    {
        var categoria = request.Categoria.Trim().ToLowerInvariant();
        var prioridad = request.Prioridad.Trim().ToLowerInvariant();

        if (!CategoriasIncidencia.Todas.Contains(categoria))
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "La categoría no es válida.");
        }

        if (!Prioridades.Todas.Contains(prioridad))
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "La prioridad no es válida.");
        }

        if (request.DetalleEspecifico.ValueKind is not JsonValueKind.Object)
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "detalleEspecifico debe ser un objeto JSON.");
        }

        var ahora = DateTime.UtcNow;
        var incidencia = new Incidencia
        {
            Codigo = GenerarCodigo(categoria),
            Titulo = request.Titulo.Trim(),
            Descripcion = request.Descripcion.Trim(),
            Categoria = categoria,
            Prioridad = prioridad,
            Estado = EstadosIncidencia.Abierta,
            Reportante = new Reportante
            {
                UsuarioId = usuario.Id,
                Nombre = usuario.Nombre,
                Tipo = usuario.TipoReportante ?? Roles.Reportante
            },
            Ubicacion = new Ubicacion
            {
                Edificio = request.Ubicacion.Edificio.Trim(),
                Area = request.Ubicacion.Area.Trim()
            },
            DetalleEspecifico = BsonDocument.Parse(
                request.DetalleEspecifico.GetRawText()),
            Tags = NormalizarTags(request.Tags),
            FechaCreacion = ahora,
            FechaActualizacion = ahora,
            HistorialSeguimiento =
            [
                new EntradaHistorial
                {
                    Accion = "incidencia_creada",
                    EstadoNuevo = EstadosIncidencia.Abierta,
                    Comentario = "Incidencia registrada.",
                    UsuarioId = usuario.Id,
                    NombreUsuario = usuario.Nombre,
                    Visibilidad = Visibilidades.Publica,
                    Fecha = ahora
                }
            ]
        };

        await ObtenerColeccion().InsertOneAsync(
            incidencia,
            cancellationToken: cancellationToken);

        return incidencia;
    }

    public async Task<Incidencia> AsignarTecnicoAsync(
        string id,
        string tecnicoId,
        UsuarioActual usuario,
        CancellationToken cancellationToken = default)
    {
        var incidencia = await ObtenerRequeridaAsync(id, usuario, cancellationToken);
        var tecnico = await _usuariosService.ObtenerPorIdAsync(
            tecnicoId,
            cancellationToken);

        if (tecnico is null || tecnico.Rol != Roles.Tecnico || !tecnico.Activo)
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "El usuario seleccionado no es un técnico activo.");
        }

        if (incidencia.Estado is EstadosIncidencia.Cerrada)
        {
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "Una incidencia cerrada no puede reasignarse.");
        }

        var ahora = DateTime.UtcNow;
        var historial = new EntradaHistorial
        {
            Accion = incidencia.TecnicoAsignado is null
                ? "tecnico_asignado"
                : "tecnico_reasignado",
            EstadoAnterior = incidencia.Estado,
            EstadoNuevo = incidencia.Estado,
            Comentario = $"Incidencia asignada a {tecnico.Nombre}.",
            UsuarioId = usuario.Id,
            NombreUsuario = usuario.Nombre,
            Visibilidad = Visibilidades.Publica,
            Fecha = ahora
        };

        var actualizacion = Builders<Incidencia>.Update
            .Set(item => item.TecnicoAsignado, new TecnicoAsignado
            {
                UsuarioId = tecnico.Id,
                Nombre = tecnico.Nombre
            })
            .Set(item => item.FechaActualizacion, ahora)
            .Push(item => item.HistorialSeguimiento, historial);

        return await ActualizarYDevolverAsync(id, usuario, actualizacion, cancellationToken);
    }

    public async Task<Incidencia> RegistrarSeguimientoAsync(
        string id,
        RegistrarSeguimientoRequest request,
        UsuarioActual usuario,
        CancellationToken cancellationToken = default)
    {
        var incidencia = await ObtenerRequeridaAsync(id, usuario, cancellationToken);
        var estadoNuevo = request.EstadoNuevo.Trim().ToLowerInvariant();

        if (!EstadosPermitidosTecnico.Contains(estadoNuevo))
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "El técnico solo puede marcar en proceso, en espera o resuelta.");
        }

        if (incidencia.Estado is EstadosIncidencia.Cerrada)
        {
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "No se pueden agregar avances a una incidencia cerrada.");
        }

        var visibilidad = NormalizarVisibilidad(request.Visibilidad, usuario);
        var ahora = DateTime.UtcNow;
        var historial = new EntradaHistorial
        {
            Accion = "avance_tecnico",
            EstadoAnterior = incidencia.Estado,
            EstadoNuevo = estadoNuevo,
            Comentario = request.Comentario.Trim(),
            UsuarioId = usuario.Id,
            NombreUsuario = usuario.Nombre,
            Visibilidad = visibilidad,
            HorasTrabajadas = request.HorasTrabajadas,
            Fecha = ahora
        };

        var actualizaciones = new List<UpdateDefinition<Incidencia>>
        {
            Builders<Incidencia>.Update.Set(item => item.Estado, estadoNuevo),
            Builders<Incidencia>.Update.Set(item => item.FechaActualizacion, ahora),
            Builders<Incidencia>.Update.Inc(item => item.HorasInvertidas, request.HorasTrabajadas),
            Builders<Incidencia>.Update.Push(item => item.HistorialSeguimiento, historial)
        };

        if (estadoNuevo == EstadosIncidencia.Resuelta)
        {
            actualizaciones.Add(
                Builders<Incidencia>.Update.Set(item => item.FechaResolucion, ahora));
        }

        return await ActualizarYDevolverAsync(
            id,
            usuario,
            Builders<Incidencia>.Update.Combine(actualizaciones),
            cancellationToken);
    }

    public async Task<Incidencia> CerrarAsync(
        string id,
        string comentario,
        UsuarioActual usuario,
        CancellationToken cancellationToken = default)
    {
        var incidencia = await ObtenerRequeridaAsync(id, usuario, cancellationToken);
        if (incidencia.Estado != EstadosIncidencia.Resuelta)
        {
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "Solo se puede cerrar una incidencia que ya fue resuelta.");
        }

        var ahora = DateTime.UtcNow;
        var historial = CrearEntradaEstado(
            "incidencia_cerrada",
            incidencia.Estado,
            EstadosIncidencia.Cerrada,
            comentario,
            usuario,
            ahora);

        var actualizacion = Builders<Incidencia>.Update
            .Set(item => item.Estado, EstadosIncidencia.Cerrada)
            .Set(item => item.FechaActualizacion, ahora)
            .Push(item => item.HistorialSeguimiento, historial);

        return await ActualizarYDevolverAsync(id, usuario, actualizacion, cancellationToken);
    }

    public async Task<Incidencia> ReabrirAsync(
        string id,
        string motivo,
        UsuarioActual usuario,
        CancellationToken cancellationToken = default)
    {
        var incidencia = await ObtenerRequeridaAsync(id, usuario, cancellationToken);
        if (incidencia.Estado is not (EstadosIncidencia.Resuelta or EstadosIncidencia.Cerrada))
        {
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "Solo se puede reabrir una incidencia resuelta o cerrada.");
        }

        var ahora = DateTime.UtcNow;
        var historial = CrearEntradaEstado(
            "incidencia_reabierta",
            incidencia.Estado,
            EstadosIncidencia.Abierta,
            motivo,
            usuario,
            ahora);

        var actualizacion = Builders<Incidencia>.Update
            .Set(item => item.Estado, EstadosIncidencia.Abierta)
            .Set(item => item.FechaActualizacion, ahora)
            .Unset(item => item.FechaResolucion)
            .Push(item => item.HistorialSeguimiento, historial);

        return await ActualizarYDevolverAsync(id, usuario, actualizacion, cancellationToken);
    }

    public async Task<EvidenciaResponse> AgregarEvidenciaAsync(
        string id,
        Stream contenido,
        string nombreArchivo,
        string tipoMime,
        long tamanoBytes,
        string? descripcion,
        string visibilidad,
        UsuarioActual usuario,
        CancellationToken cancellationToken = default)
    {
        await ObtenerRequeridaAsync(id, usuario, cancellationToken);

        if (tamanoBytes <= 0 || tamanoBytes > TamanoMaximoEvidencia)
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "La evidencia debe pesar entre 1 byte y 5 MB.");
        }

        if (!TiposEvidenciaPermitidos.Contains(tipoMime))
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "Solo se permiten archivos JPG, PNG o PDF.");
        }

        var visibilidadNormalizada = NormalizarVisibilidad(visibilidad, usuario);
        var ruta = await _storageService.SubirAsync(
            contenido,
            nombreArchivo,
            tipoMime,
            id,
            cancellationToken);

        var evidencia = new EvidenciaAdjunta
        {
            NombreArchivo = Path.GetFileName(nombreArchivo),
            TipoMime = tipoMime,
            TamanoBytes = tamanoBytes,
            RutaStorage = ruta,
            Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim(),
            UsuarioId = usuario.Id,
            NombreUsuario = usuario.Nombre,
            Visibilidad = visibilidadNormalizada,
            FechaCarga = DateTime.UtcNow
        };

        var actualizacion = Builders<Incidencia>.Update
            .Set(item => item.FechaActualizacion, DateTime.UtcNow)
            .Push(item => item.EvidenciasAdjuntas, evidencia);

        await ActualizarYDevolverAsync(id, usuario, actualizacion, cancellationToken);
        var urlTemporal = await _storageService.CrearUrlTemporalAsync(
            evidencia.RutaStorage,
            cancellationToken);

        return MapearEvidencia(evidencia, urlTemporal);
    }

    public async Task<IReadOnlyList<EvidenciaResponse>> ObtenerEvidenciasAsync(
        string id,
        UsuarioActual usuario,
        CancellationToken cancellationToken = default)
    {
        var incidencia = await ObtenerRequeridaAsync(id, usuario, cancellationToken);
        var evidencias = usuario.Rol == Roles.Reportante
            ? incidencia.EvidenciasAdjuntas
                .Where(item => item.Visibilidad != Visibilidades.Interna)
            : incidencia.EvidenciasAdjuntas;

        var resultado = new List<EvidenciaResponse>();
        foreach (var evidencia in evidencias)
        {
            var url = evidencia.RutaStorage.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? evidencia.RutaStorage
                : await _storageService.CrearUrlTemporalAsync(
                    evidencia.RutaStorage,
                    cancellationToken);
            resultado.Add(MapearEvidencia(evidencia, url));
        }

        return resultado;
    }

    public async Task<AnaliticaResponse> ObtenerAnaliticaAsync(
        UsuarioActual usuario,
        CancellationToken cancellationToken = default)
    {
        var incidencias = await ObtenerColeccion()
            .Find(ConstruirFiltroAcceso(usuario))
            .ToListAsync(cancellationToken);

        static IReadOnlyList<ConteoAgrupadoResponse> Agrupar(IEnumerable<string> valores) =>
            valores
                .Where(valor => !string.IsNullOrWhiteSpace(valor))
                .GroupBy(valor => valor, StringComparer.OrdinalIgnoreCase)
                .Select(grupo => new ConteoAgrupadoResponse(grupo.Key, grupo.Count()))
                .OrderByDescending(item => item.Total)
                .ThenBy(item => item.Nombre)
                .ToList();

        return new AnaliticaResponse(
            incidencias.Count,
            incidencias.Sum(item => item.HorasInvertidas),
            Agrupar(incidencias.Select(item => item.Estado)),
            Agrupar(incidencias.Select(item => item.Categoria)),
            Agrupar(incidencias.Select(item => item.Ubicacion.Edificio)),
            Agrupar(incidencias.SelectMany(item => item.Tags)).Take(10).ToList());
    }

    private async Task<Incidencia> ObtenerRequeridaAsync(
        string id,
        UsuarioActual usuario,
        CancellationToken cancellationToken)
    {
        var incidencia = await ObtenerPorIdAsync(id, usuario, cancellationToken);
        return incidencia ?? throw new ApiException(
            StatusCodes.Status404NotFound,
            "La incidencia no existe o no tienes permiso para consultarla.");
    }

    private async Task<Incidencia> ActualizarYDevolverAsync(
        string id,
        UsuarioActual usuario,
        UpdateDefinition<Incidencia> actualizacion,
        CancellationToken cancellationToken)
    {
        var filtro = Builders<Incidencia>.Filter.And(
            Builders<Incidencia>.Filter.Eq(item => item.Id, id),
            ConstruirFiltroAcceso(usuario));

        var resultado = await ObtenerColeccion().FindOneAndUpdateAsync(
            filtro,
            actualizacion,
            new FindOneAndUpdateOptions<Incidencia>
            {
                ReturnDocument = ReturnDocument.After
            },
            cancellationToken);

        return resultado ?? throw new ApiException(
            StatusCodes.Status404NotFound,
            "La incidencia no existe o no tienes permiso para modificarla.");
    }

    private FilterDefinition<Incidencia> ConstruirFiltroAcceso(UsuarioActual usuario)
    {
        var filtros = Builders<Incidencia>.Filter;
        return usuario.Rol switch
        {
            Roles.Reportante => filtros.Eq(
                item => item.Reportante.UsuarioId,
                usuario.Id),
            Roles.Tecnico => filtros.Eq(
                item => item.TecnicoAsignado!.UsuarioId,
                usuario.Id),
            Roles.Coordinador when !string.IsNullOrWhiteSpace(usuario.Area) =>
                filtros.Eq(
                    item => item.Categoria,
                    NormalizarAreaCoordinador(usuario.Area)),
            Roles.Directivo or Roles.SuperAdmin => filtros.Empty,
            _ => new BsonDocumentFilterDefinition<Incidencia>(
                new BsonDocument("_id", ObjectId.Empty))
        };
    }

    private static string NormalizarAreaCoordinador(string area)
    {
        var normalizada = area.Trim().ToLowerInvariant();
        if (normalizada.Contains("infraestructura"))
        {
            return CategoriasIncidencia.Infraestructura;
        }

        if (normalizada.Contains("cómputo") ||
            normalizada.Contains("computo") ||
            normalizada.Contains("red"))
        {
            return CategoriasIncidencia.ComputoRedes;
        }

        if (normalizada.Contains("académ") || normalizada.Contains("academ"))
        {
            return CategoriasIncidencia.ServiciosAcademicos;
        }

        return normalizada.Replace(' ', '_');
    }

    private static List<string> NormalizarTags(IReadOnlyList<string>? tags) =>
        tags?
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim().TrimStart('#').ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToList() ?? [];

    private static string NormalizarVisibilidad(
        string? visibilidad,
        UsuarioActual usuario)
    {
        if (usuario.Rol == Roles.Reportante)
        {
            return Visibilidades.Publica;
        }

        return string.Equals(
            visibilidad,
            Visibilidades.Interna,
            StringComparison.OrdinalIgnoreCase)
            ? Visibilidades.Interna
            : Visibilidades.Publica;
    }

    private static EntradaHistorial CrearEntradaEstado(
        string accion,
        string estadoAnterior,
        string estadoNuevo,
        string comentario,
        UsuarioActual usuario,
        DateTime fecha) =>
        new()
        {
            Accion = accion,
            EstadoAnterior = estadoAnterior,
            EstadoNuevo = estadoNuevo,
            Comentario = comentario.Trim(),
            UsuarioId = usuario.Id,
            NombreUsuario = usuario.Nombre,
            Visibilidad = Visibilidades.Publica,
            Fecha = fecha
        };

    private static string GenerarCodigo(string categoria)
    {
        var prefijo = categoria switch
        {
            CategoriasIncidencia.ComputoRedes => "CR",
            CategoriasIncidencia.Infraestructura => "INF",
            CategoriasIncidencia.ServiciosAcademicos => "SA",
            _ => "GEN"
        };

        return $"INC-{DateTime.UtcNow:yyyyMMdd}-{prefijo}-{RandomNumberGenerator.GetHexString(3)}";
    }

    private static EvidenciaResponse MapearEvidencia(
        EvidenciaAdjunta evidencia,
        string urlTemporal) =>
        new(
            evidencia.NombreArchivo,
            evidencia.TipoMime,
            evidencia.TamanoBytes,
            evidencia.Descripcion,
            evidencia.NombreUsuario,
            evidencia.FechaCarga,
            urlTemporal);

    private IMongoCollection<Incidencia> ObtenerColeccion() =>
        _incidencias ?? throw new InvalidOperationException(
            "Falta configurar MongoDb:ConnectionString en appsettings.Local.json.");
}
