using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
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

public sealed class UsuariosService : IUsuariosService
{
    private readonly IMongoCollection<Usuario>? _usuarios;
    private readonly IPasswordHasher<Usuario> _passwordHasher;
    private readonly SuperAdminSeedSettings _seedSettings;
    private readonly ILogger<UsuariosService> _logger;

    public UsuariosService(
        IOptions<MongoDbSettings> mongoOptions,
        IOptions<SuperAdminSeedSettings> seedOptions,
        IPasswordHasher<Usuario> passwordHasher,
        ILogger<UsuariosService> logger)
    {
        _passwordHasher = passwordHasher;
        _seedSettings = seedOptions.Value;
        _logger = logger;

        var settings = mongoOptions.Value;
        if (string.IsNullOrWhiteSpace(settings.ConnectionString))
        {
            return;
        }

        var cliente = new MongoClient(settings.ConnectionString);
        _usuarios = cliente
            .GetDatabase(settings.DatabaseName)
            .GetCollection<Usuario>(settings.UsuariosCollectionName);
    }

    public async Task<Usuario?> ObtenerPorEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var usuarios = ObtenerColeccion();
        var emailSeguro = Regex.Escape(email.Trim());
        var filtro = Builders<Usuario>.Filter.Regex(
            usuario => usuario.Email,
            new BsonRegularExpression($"^{emailSeguro}$", "i"));

        return await usuarios.Find(filtro).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Usuario?> ObtenerPorIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return null;
        }

        return await ObtenerColeccion()
            .Find(usuario => usuario.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Usuario>> ObtenerTecnicosAsync(
        string? especialidad,
        CancellationToken cancellationToken = default)
    {
        var filtro = Builders<Usuario>.Filter.And(
            Builders<Usuario>.Filter.Eq(usuario => usuario.Rol, Roles.Tecnico),
            Builders<Usuario>.Filter.Eq(usuario => usuario.Activo, true));

        if (!string.IsNullOrWhiteSpace(especialidad))
        {
            filtro &= Builders<Usuario>.Filter.Eq(
                usuario => usuario.Especialidad,
                especialidad.Trim());
        }

        return await ObtenerColeccion()
            .Find(filtro)
            .SortBy(usuario => usuario.Nombre)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Usuario>> ObtenerTodosAsync(
        CancellationToken cancellationToken = default) =>
        await ObtenerColeccion()
            .Find(Builders<Usuario>.Filter.Empty)
            .SortBy(usuario => usuario.Nombre)
            .ToListAsync(cancellationToken);

    public async Task<Usuario> CrearAsync(
        CrearUsuarioRequest request,
        CancellationToken cancellationToken = default)
    {
        var rol = request.Rol.Trim();
        if (!Roles.Todos.Contains(rol))
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "El rol indicado no es válido.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await ObtenerPorEmailAsync(email, cancellationToken) is not null)
        {
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "Ya existe un usuario con ese correo.");
        }

        if (rol == Roles.Coordinador && string.IsNullOrWhiteSpace(request.Area))
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "Un coordinador debe tener un área/categoría asignada.");
        }

        if (rol == Roles.Reportante &&
            (string.IsNullOrWhiteSpace(request.TipoReportante) ||
             !TiposReportante.Todos.Contains(request.TipoReportante.Trim())))
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "Un reportante debe indicar tipo: estudiante, docente o administrativo.");
        }

        if (rol == Roles.Tecnico && string.IsNullOrWhiteSpace(request.Especialidad))
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "Un técnico debe tener una especialidad.");
        }

        var usuario = new Usuario
        {
            Nombre = request.Nombre.Trim(),
            Email = email,
            Rol = rol,
            TipoReportante = Limpiar(request.TipoReportante),
            Area = Limpiar(request.Area),
            Especialidad = Limpiar(request.Especialidad),
            Activo = request.Activo,
            FechaCreacion = DateTime.UtcNow
        };

        usuario.PasswordHash = _passwordHasher.HashPassword(
            usuario,
            request.Password);

        await ObtenerColeccion().InsertOneAsync(
            usuario,
            cancellationToken: cancellationToken);

        return usuario;
    }

    public async Task<Usuario?> ActualizarAsync(
        string id,
        ActualizarUsuarioRequest request,
        UsuarioActual usuario,
        CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return null;
        }

        var existente = await ObtenerPorIdAsync(id, cancellationToken);
        if (existente is null)
        {
            return null;
        }

        var rol = request.Rol.Trim();
        if (!Roles.Todos.Contains(rol))
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "El rol indicado no es válido.");
        }

        var esCuentaActual = string.Equals(
            existente.Id,
            usuario.Id,
            StringComparison.Ordinal);

        if (esCuentaActual && rol != Roles.SuperAdmin)
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "No puedes cambiar el rol de tu propia cuenta de Super Administrador.");
        }

        if (esCuentaActual && !request.Activo)
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "No puedes desactivar tu propia cuenta.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var otro = await ObtenerPorEmailAsync(email, cancellationToken);
        if (otro is not null &&
            !string.Equals(otro.Id, id, StringComparison.Ordinal))
        {
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "Ya existe un usuario con ese correo.");
        }

        if (rol == Roles.Coordinador && string.IsNullOrWhiteSpace(request.Area))
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "Un coordinador debe tener un área/categoría asignada.");
        }

        if (rol == Roles.Reportante &&
            (string.IsNullOrWhiteSpace(request.TipoReportante) ||
             !TiposReportante.Todos.Contains(request.TipoReportante.Trim())))
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "Un reportante debe indicar tipo: estudiante, docente o administrativo.");
        }

        if (rol == Roles.Tecnico && string.IsNullOrWhiteSpace(request.Especialidad))
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "Un técnico debe tener una especialidad.");
        }

        if (existente.Rol == Roles.SuperAdmin &&
            (!request.Activo || rol != Roles.SuperAdmin))
        {
            var superAdminsActivos = await ObtenerColeccion().CountDocumentsAsync(
                item => item.Rol == Roles.SuperAdmin && item.Activo,
                cancellationToken: cancellationToken);

            if (superAdminsActivos <= 1)
            {
                throw new ApiException(
                    StatusCodes.Status409Conflict,
                    "Debe existir al menos un Super Administrador activo.");
            }
        }

        var tipoReportante = Limpiar(request.TipoReportante);
        var area = Limpiar(request.Area);
        var especialidad = Limpiar(request.Especialidad);

        var actualizaciones = new List<UpdateDefinition<Usuario>>
        {
            Builders<Usuario>.Update.Set(item => item.Nombre, request.Nombre.Trim()),
            Builders<Usuario>.Update.Set(item => item.Email, email),
            Builders<Usuario>.Update.Set(item => item.Rol, rol),
            Builders<Usuario>.Update.Set(item => item.Activo, request.Activo),
            tipoReportante is null
                ? Builders<Usuario>.Update.Unset(item => item.TipoReportante)
                : Builders<Usuario>.Update.Set(item => item.TipoReportante, tipoReportante),
            area is null
                ? Builders<Usuario>.Update.Unset(item => item.Area)
                : Builders<Usuario>.Update.Set(item => item.Area, area),
            especialidad is null
                ? Builders<Usuario>.Update.Unset(item => item.Especialidad)
                : Builders<Usuario>.Update.Set(item => item.Especialidad, especialidad)
        };

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            actualizaciones.Add(Builders<Usuario>.Update.Set(
                item => item.PasswordHash,
                _passwordHasher.HashPassword(existente, request.Password)));
        }

        return await ObtenerColeccion().FindOneAndUpdateAsync(
            item => item.Id == id,
            Builders<Usuario>.Update.Combine(actualizaciones),
            new FindOneAndUpdateOptions<Usuario>
            {
                ReturnDocument = ReturnDocument.After
            },
            cancellationToken);
    }

    public async Task<bool> EliminarAsync(
        string id,
        UsuarioActual usuario,
        CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return false;
        }

        var existente = await ObtenerPorIdAsync(id, cancellationToken);
        if (existente is null)
        {
            return false;
        }

        if (string.Equals(existente.Id, usuario.Id, StringComparison.Ordinal))
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "No puedes eliminar tu propia cuenta.");
        }

        if (existente.Rol == Roles.SuperAdmin && existente.Activo)
        {
            var superAdminsActivos = await ObtenerColeccion().CountDocumentsAsync(
                item => item.Rol == Roles.SuperAdmin && item.Activo,
                cancellationToken: cancellationToken);

            if (superAdminsActivos <= 1)
            {
                throw new ApiException(
                    StatusCodes.Status409Conflict,
                    "Debe existir al menos un Super Administrador activo.");
            }
        }

        var resultado = await ObtenerColeccion().DeleteOneAsync(
            item => item.Id == id,
            cancellationToken);

        return resultado.DeletedCount == 1;
    }

    public async Task<Usuario?> CambiarEstadoAsync(
        string id,
        bool activo,
        CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return null;
        }

        return await ObtenerColeccion().FindOneAndUpdateAsync(
            usuario => usuario.Id == id,
            Builders<Usuario>.Update.Set(usuario => usuario.Activo, activo),
            new FindOneAndUpdateOptions<Usuario>
            {
                ReturnDocument = ReturnDocument.After
            },
            cancellationToken);
    }

    public async Task<bool> CambiarPasswordAsync(
        string id,
        string password,
        CancellationToken cancellationToken = default)
    {
        var usuario = await ObtenerPorIdAsync(id, cancellationToken);
        if (usuario is null)
        {
            return false;
        }

        var hash = _passwordHasher.HashPassword(usuario, password);
        var resultado = await ObtenerColeccion().UpdateOneAsync(
            item => item.Id == id,
            Builders<Usuario>.Update.Set(item => item.PasswordHash, hash),
            cancellationToken: cancellationToken);

        return resultado.ModifiedCount == 1;
    }

    public async Task CrearSuperAdminSiNoExisteAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_seedSettings.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_seedSettings.Nombre) ||
            string.IsNullOrWhiteSpace(_seedSettings.Email) ||
            string.IsNullOrWhiteSpace(_seedSettings.Password))
        {
            throw new InvalidOperationException(
                "SuperAdminSeed está habilitado, pero faltan nombre, correo o contraseña.");
        }

        var existente = await ObtenerPorEmailAsync(
            _seedSettings.Email,
            cancellationToken);

        if (existente is not null)
        {
            _logger.LogInformation(
                "El usuario SuperAdmin {Email} ya existe; no se modificó.",
                _seedSettings.Email);
            return;
        }

        var usuario = new Usuario
        {
            Nombre = _seedSettings.Nombre.Trim(),
            Email = _seedSettings.Email.Trim().ToLowerInvariant(),
            Rol = Roles.SuperAdmin,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        usuario.PasswordHash = _passwordHasher.HashPassword(
            usuario,
            _seedSettings.Password);

        await ObtenerColeccion().InsertOneAsync(
            usuario,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Se creó el usuario SuperAdmin {Email}.",
            usuario.Email);
    }

    private IMongoCollection<Usuario> ObtenerColeccion() =>
        _usuarios ?? throw new InvalidOperationException(
            "Falta configurar MongoDb:ConnectionString en appsettings.Local.json.");

    private static string? Limpiar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
