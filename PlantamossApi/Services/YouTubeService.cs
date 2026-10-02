using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using PlantamossApi.DTOs;

namespace PlantamossApi.Services;

/// <summary>
/// Proxy hacia YouTube Data API v3 (búsqueda de videos de flora mexicana).
/// La API key vive SOLO en el servidor (YouTube:ApiKey). Caché de 6 horas para no gastar cuota.
/// Nunca lanza excepción: si algo falla devuelve una lista vacía.
/// </summary>
public class YouTubeService(
    IHttpClientFactory httpFactory,
    IMemoryCache cache,
    IConfiguration config,
    ILogger<YouTubeService> logger)
{
    public const string NombreHttpClient = "youtube";
    private const string ClaveCache = "videos:flora";
    private const string Busqueda = "plantas mexicanas flora México naturaleza";

    public async Task<List<VideoDto>> ObtenerAsync()
    {
        if (cache.TryGetValue(ClaveCache, out List<VideoDto>? enCache) && enCache is not null)
            return enCache;

        var apiKey = config["YouTube:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogWarning("YouTube:ApiKey no está configurada; /externo/videos devuelve [].");
            return [];
        }

        try
        {
            var cliente = httpFactory.CreateClient(NombreHttpClient);
            var url = "youtube/v3/search?part=snippet&type=video&maxResults=6"
                    + "&relevanceLanguage=es&regionCode=MX"
                    + $"&q={Uri.EscapeDataString(Busqueda)}&key={apiKey}";
            using var respuesta = await cliente.GetAsync(url);

            if (!respuesta.IsSuccessStatusCode)
            {
                logger.LogWarning("YouTube respondió {Status}", (int)respuesta.StatusCode);
                // Cacheamos el fallo poco tiempo para no agotar la cuota con reintentos
                cache.Set(ClaveCache, new List<VideoDto>(), TimeSpan.FromMinutes(5));
                return [];
            }

            using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
            var videos = new List<VideoDto>();

            foreach (var item in json.RootElement.GetProperty("items").EnumerateArray())
            {
                if (!item.GetProperty("id").TryGetProperty("videoId", out var idVideo)) continue;
                var snippet = item.GetProperty("snippet");

                string? miniatura = null;
                if (snippet.TryGetProperty("thumbnails", out var thumbs))
                {
                    foreach (var calidad in new[] { "high", "medium", "default" })
                    {
                        if (thumbs.TryGetProperty(calidad, out var t)) { miniatura = t.GetProperty("url").GetString(); break; }
                    }
                }

                DateTime? fecha = snippet.TryGetProperty("publishedAt", out var pub) && pub.TryGetDateTime(out var f)
                    ? f.ToUniversalTime() : null;

                videos.Add(new VideoDto(
                    idVideo.GetString() ?? "",
                    // YouTube devuelve los títulos con entidades HTML (&quot;, &#39;...)
                    WebUtility.HtmlDecode(snippet.GetProperty("title").GetString() ?? ""),
                    WebUtility.HtmlDecode(snippet.GetProperty("channelTitle").GetString() ?? ""),
                    miniatura,
                    fecha));
            }

            cache.Set(ClaveCache, videos, TimeSpan.FromHours(6));
            return videos;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error consultando YouTube");
            return [];
        }
    }
}
