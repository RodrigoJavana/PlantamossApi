namespace PlantamossApi.Middleware;

/// <summary>
/// Cabeceras de seguridad en TODAS las respuestas (endurecimiento, punto 6):
/// <list type="bullet">
/// <item><c>X-Content-Type-Options: nosniff</c> — el navegador no "adivina" el tipo de contenido.</item>
/// <item><c>X-Frame-Options: DENY</c> + CSP <c>frame-ancestors 'none'</c> — evita clickjacking.</item>
/// <item><c>Referrer-Policy: no-referrer</c> — no se filtran URLs a terceros.</item>
/// <item><c>Permissions-Policy</c> — desactiva cámara, micrófono, geolocalización, etc.</item>
/// <item><c>Content-Security-Policy</c> — la API solo devuelve JSON, así que se prohíbe todo (<c>default-src 'none'</c>).
///       Para /swagger (solo si está habilitado) se usa una política menos estricta que permite su HTML/JS.</item>
/// <item><c>Cache-Control: no-store</c> en /api/auth y /api/perfil (respuestas con tokens o datos personales).</item>
/// </list>
/// </summary>
public class CabecerasSeguridadMiddleware(RequestDelegate next)
{
    private const string CspApi =
        "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";

    private const string CspSwagger =
        "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data: https:; font-src 'self' data:; connect-src 'self'; " +
        "frame-ancestors 'none'; base-uri 'self'; form-action 'self'";

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var h = context.Response.Headers;
            var ruta = context.Request.Path;

            h["X-Content-Type-Options"] = "nosniff";
            h["X-Frame-Options"] = "DENY";
            h["Referrer-Policy"] = "no-referrer";
            h["Permissions-Policy"] = "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()";
            h["Cross-Origin-Opener-Policy"] = "same-origin";
            h["Content-Security-Policy"] = ruta.StartsWithSegments("/swagger") ? CspSwagger : CspApi;
            h.Remove("Server");
            h.Remove("X-Powered-By");

            if (ruta.StartsWithSegments("/api/auth") || ruta.StartsWithSegments("/api/perfil"))
            {
                h.CacheControl = "no-store";
                h.Pragma = "no-cache";
            }
            return Task.CompletedTask;
        });

        return next(context);
    }
}
