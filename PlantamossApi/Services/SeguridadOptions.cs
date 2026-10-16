namespace PlantamossApi.Services;

/// <summary>
/// Configuración de la sección "Seguridad" (appsettings / .env / variables de entorno).
/// </summary>
public class SeguridadOptions
{
    /// <summary>
    /// "Pepper" secreto para el HMAC-SHA256 de los códigos de 6 dígitos (PEPPER_CODIGOS en el .env).
    /// Si está vacío se deriva de Jwt:Key (con aviso en el log).
    /// </summary>
    public string PepperCodigos { get; set; } = "";

    /// <summary>Si es true, las peticiones que no llegan por HTTPS se rechazan (producción).</summary>
    public bool RequireHttps { get; set; }

    /// <summary>
    /// True cuando la API corre detrás de un proxy/balanceador (Azure App Service):
    /// se confía en X-Forwarded-For / X-Forwarded-Proto de cualquier origen.
    /// En false solo se confía en los que vienen de localhost.
    /// </summary>
    public bool DetrasDeProxy { get; set; }

    /// <summary>Tamaño máximo del cuerpo de una petición (bytes). 1 MB por defecto.</summary>
    public long MaxBodyBytes { get; set; } = 1_048_576;

    /// <summary>Intentos fallidos de login por correo antes del bloqueo temporal.</summary>
    public int MaxIntentosLogin { get; set; } = 5;

    /// <summary>Minutos que dura el bloqueo temporal de la cuenta (y la ventana en la que se cuentan los fallos).</summary>
    public int MinutosBloqueo { get; set; } = 15;

    /// <summary>Días de vida del refresh token (CONTRATO 5.1: 7).</summary>
    public int DiasRefreshToken { get; set; } = 7;
}
