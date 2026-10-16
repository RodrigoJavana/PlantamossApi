using PlantamossApi.DTOs;

namespace PlantamossApi.Middleware;

/// <summary>
/// Atrapa cualquier excepción no controlada, la escribe en el log (con su TraceId) y responde
/// 500 <c>{ "message": "Ocurrió un error inesperado.", "traceId": "..." }</c>
/// sin filtrar detalles internos (stack trace, SQL, rutas) al cliente.
/// El <c>traceId</c> es el mismo valor del header X-Correlation-Id: con él soporte encuentra el error en logs/.
/// </summary>
public class ExcepcionesMiddleware(RequestDelegate next, ILogger<ExcepcionesMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex) when (ex is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
        {
            // El cliente cerró la conexión: no es un error del servidor
            logger.LogDebug("Petición cancelada por el cliente: {Ruta}", context.Request.Path);
        }
        catch (BadHttpRequestException ex)
        {
            // Errores del protocolo HTTP: cuerpo demasiado grande (413), cabeceras inválidas, etc.
            logger.LogWarning("Petición HTTP inválida en {Metodo} {Ruta}: {Status} {Motivo}",
                context.Request.Method, context.Request.Path, ex.StatusCode, ex.Message);
            if (context.Response.HasStarted) throw;

            LimpiarConservandoCabeceras(context.Response);
            context.Response.StatusCode = ex.StatusCode;
            await context.Response.WriteAsJsonAsync(new ApiError(ex.StatusCode == StatusCodes.Status413PayloadTooLarge
                ? "El cuerpo de la petición es demasiado grande."
                : "La petición HTTP no es válida."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error no controlado en {Metodo} {Ruta} (traceId {TraceId})",
                context.Request.Method, context.Request.Path, context.TraceIdentifier);

            if (context.Response.HasStarted) throw;

            LimpiarConservandoCabeceras(context.Response);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(
                new ApiError("Ocurrió un error inesperado.", traceId: context.TraceIdentifier));
        }
    }

    /// <summary>
    /// Limpia la respuesta para escribir el JSON de error, pero CONSERVA las cabeceras que ya pusieron
    /// otros middlewares (CORS Access-Control-*, Vary, X-Correlation-Id y cabeceras de seguridad).
    /// Si se perdieran las de CORS, el navegador mostraría un "error de CORS" en vez del error real.
    /// </summary>
    private static void LimpiarConservandoCabeceras(HttpResponse response)
    {
        var conservar = response.Headers
            .Where(h => !h.Key.StartsWith("Content-", StringComparison.OrdinalIgnoreCase))
            .ToList();
        response.Clear();
        foreach (var (clave, valor) in conservar)
            response.Headers[clave] = valor;
    }
}
