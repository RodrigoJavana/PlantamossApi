using System.Diagnostics;
using Serilog.Context;

namespace PlantamossApi.Middleware;

/// <summary>
/// Id de correlación por petición (header <c>X-Correlation-Id</c>):
/// <list type="bullet">
/// <item>Si el cliente manda uno válido (máx. 64 caracteres: letras, números, '-', '_', '.') se reutiliza;
///       si no, se usa el TraceId de la actividad actual (W3C).</item>
/// <item>Se devuelve SIEMPRE en la respuesta y se agrega a todos los logs de la petición (propiedad CorrelationId).</item>
/// <item>Se guarda en <c>HttpContext.TraceIdentifier</c>: el 500 lo devuelve como <c>traceId</c>.</item>
/// </list>
/// </summary>
public class CorrelacionMiddleware(RequestDelegate next)
{
    public const string Header = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var id = context.Request.Headers[Header].ToString();
        if (!EsValido(id))
            id = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;

        context.TraceIdentifier = id;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[Header] = id;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", id))
        {
            await next(context);
        }
    }

    private static bool EsValido(string id) =>
        id.Length is > 0 and <= 64 && id.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.');
}
