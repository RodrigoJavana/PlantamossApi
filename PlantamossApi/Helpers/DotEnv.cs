namespace PlantamossApi.Helpers;

/// <summary>
/// Lee el archivo <c>.env</c> de ApiPlantamoss (el mismo que usa docker compose) para que
/// <c>dotnet run</c> también tome los SECRETOS (clave JWT, pepper, cadena de conexión, API keys, SMTP)
/// sin tenerlos en appsettings (que sí se sube a git). El .env está en .gitignore.
/// Busca el archivo en la carpeta del proyecto y en la carpeta padre.
/// </summary>
public static class DotEnv
{
    // Variable del .env  →  clave de configuración de .NET
    private static readonly Dictionary<string, string> Mapeo = new(StringComparer.OrdinalIgnoreCase)
    {
        ["JWT_KEY"] = "Jwt:Key",
        ["DB_CONNECTION"] = "ConnectionStrings:Default",
        ["PEPPER_CODIGOS"] = "Seguridad:PepperCodigos",
        ["CORS_ORIGIN"] = "Cors:Origins:0",
        ["OPENWEATHER_API_KEY"] = "OpenWeather:ApiKey",
        ["YOUTUBE_API_KEY"] = "YouTube:ApiKey",
        ["SMTP_HOST"] = "Smtp:Host",
        ["SMTP_PORT"] = "Smtp:Port",
        ["SMTP_USER"] = "Smtp:User",
        ["SMTP_PASSWORD"] = "Smtp:Password",
        ["SMTP_FROM"] = "Smtp:From",
        ["SMTP_ENABLE_SSL"] = "Smtp:EnableSsl",
    };

    /// <summary>Agrega los valores del .env a la configuración (las variables de entorno reales siguen ganando).</summary>
    public static void Cargar(ConfigurationManager config, string contentRoot)
    {
        var ruta = new[] { Path.Combine(contentRoot, ".env"), Path.Combine(contentRoot, "..", ".env") }
            .FirstOrDefault(File.Exists);
        if (ruta is null) return;

        var valores = new Dictionary<string, string?>();
        foreach (var linea in File.ReadAllLines(ruta))
        {
            var texto = linea.Trim();
            if (texto.Length == 0 || texto.StartsWith('#')) continue;

            var igual = texto.IndexOf('=');
            if (igual <= 0) continue;

            var nombre = texto[..igual].Trim();
            var valor = texto[(igual + 1)..].Trim().Trim('"', '\'');

            // Los valores vacíos no pisan lo que ya tenga appsettings
            if (valor.Length > 0 && Mapeo.TryGetValue(nombre, out var clave))
                valores[clave] = valor;
        }

        config.AddInMemoryCollection(valores);
        config.AddEnvironmentVariables();
    }
}
