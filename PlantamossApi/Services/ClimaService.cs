using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using PlantamossApi.DTOs;

namespace PlantamossApi.Services;

/// <summary>
/// Proxy hacia OpenWeather (clima actual). La API key vive SOLO en el servidor (OpenWeather:ApiKey).
/// Resultado en caché 10 minutos por ciudad. Nunca lanza excepción: si algo falla devuelve ok=false.
/// </summary>
public class ClimaService(
    IHttpClientFactory httpFactory,
    IMemoryCache cache,
    IConfiguration config,
    ILogger<ClimaService> logger)
{
    public const string NombreHttpClient = "openweather";

    public async Task<ClimaDto> ObtenerAsync(string ciudad)
    {
        ciudad = string.IsNullOrWhiteSpace(ciudad) ? "Mexico City,MX" : ciudad.Trim();
        var claveCache = "clima:" + ciudad.ToLowerInvariant();

        if (cache.TryGetValue(claveCache, out ClimaDto? enCache) && enCache is not null)
            return enCache;

        var apiKey = config["OpenWeather:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogWarning("OpenWeather:ApiKey no está configurada; /externo/clima devuelve ok=false.");
            return ClimaDto.Fallo();
        }

        try
        {
            var cliente = httpFactory.CreateClient(NombreHttpClient);
            var url = $"data/2.5/weather?q={Uri.EscapeDataString(ciudad)}&units=metric&lang=es&appid={apiKey}";
            using var respuesta = await cliente.GetAsync(url);

            if (!respuesta.IsSuccessStatusCode)
            {
                logger.LogWarning("OpenWeather respondió {Status} para '{Ciudad}'", (int)respuesta.StatusCode, ciudad);
                return ClimaDto.Fallo();
            }

            using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
            var raiz = json.RootElement;
            var main = raiz.GetProperty("main");
            var clima = raiz.GetProperty("weather")[0];

            var resultado = new ClimaDto(
                Ok: true,
                Temp: main.GetProperty("temp").GetDouble(),
                Sensacion: main.GetProperty("feels_like").GetDouble(),
                Humedad: main.GetProperty("humidity").GetInt32(),
                Viento: raiz.TryGetProperty("wind", out var viento) && viento.TryGetProperty("speed", out var vel)
                    ? vel.GetDouble() : null,
                Descripcion: clima.GetProperty("description").GetString(),
                Icono: clima.GetProperty("icon").GetString());

            cache.Set(claveCache, resultado, TimeSpan.FromMinutes(10));
            return resultado;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error consultando OpenWeather para '{Ciudad}'", ciudad);
            return ClimaDto.Fallo();
        }
    }
}
