using Microsoft.AspNetCore.Mvc;
using PlantamossApi.DTOs;
using PlantamossApi.Helpers;
using PlantamossApi.Services;

namespace PlantamossApi.Controllers;

/// <summary>
/// Proxy a servicios externos (OpenWeather y YouTube). Así las API keys nunca llegan al navegador.
/// Estos endpoints nunca devuelven 500: si el servicio externo falla responden ok=false o [].
/// </summary>
[Route("api/externo")]
public class ExternoController(ClimaService clima, YouTubeService youtube) : ApiControllerBase
{
    // GET /api/externo/clima?q=Mexico City,MX
    [HttpGet("clima")]
    public Task<ClimaDto> Clima([FromQuery, System.ComponentModel.DataAnnotations.MaxLength(100, ErrorMessage = "La ciudad no puede superar 100 caracteres.")] string? q) => clima.ObtenerAsync(q ?? "");

    // GET /api/externo/ciudades   → catálogo de ciudades que se pueden fijar en el clima de Inicio
    [HttpGet("ciudades")]
    public CiudadClima[] Ciudades() => CiudadesClima.Todas;

    // GET /api/externo/videos
    [HttpGet("videos")]
    public Task<List<VideoDto>> Videos() => youtube.ObtenerAsync();
}
