using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace UTMDesk.Api.Exceptions;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            ApiException api => (api.StatusCode, "La operación no pudo completarse", api.Message),
            MongoException => (
                StatusCodes.Status503ServiceUnavailable,
                "MongoDB Atlas no está disponible",
                "Revisa la conexión y la lista de acceso de red en Atlas."),
            InvalidOperationException invalidOperation => (
                StatusCodes.Status503ServiceUnavailable,
                "El servicio no está configurado",
                invalidOperation.Message),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Error interno",
                "Ocurrió un error inesperado.")
        };

        if (status >= 500)
        {
            _logger.LogError(exception, "Error al procesar {Path}", httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = status;
        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail
            },
            Exception = exception
        });
    }
}
