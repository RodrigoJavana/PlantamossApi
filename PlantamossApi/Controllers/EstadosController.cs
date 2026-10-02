using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlantamossApi.Data;
using PlantamossApi.DTOs;
using PlantamossApi.Helpers;

namespace PlantamossApi.Controllers;

/// <summary>Estados de México (para el mapa interactivo y los filtros del catálogo). Público.</summary>
[Route("api/estados")]
public class EstadosController(AppDbContext db) : ApiControllerBase
{
    // GET /api/estados
    [HttpGet]
    public async Task<List<EstadoDto>> Listar() =>
        await db.Estados
            .OrderBy(e => e.NombreEstado)
            .Select(Mapeos.EstadoProyeccion)
            .ToListAsync();

    // GET /api/estados/{id}/plantas
    [HttpGet("{id:int}/plantas")]
    public async Task<ActionResult<EstadoConPlantasDto>> Plantas(int id)
    {
        var estado = await db.Estados
            .Where(e => e.IdEstado == id)
            .Select(Mapeos.EstadoProyeccion)
            .FirstOrDefaultAsync();

        if (estado is null) return NoEncontrado("Estado no encontrado.");

        var plantas = await db.Plantas
            .Where(p => p.IdEstado == id)
            .OrderBy(p => p.LetraInicial).ThenBy(p => p.NombrePlanta)
            .Select(Mapeos.PlantaProyeccion)
            .ToListAsync();

        return new EstadoConPlantasDto(estado, plantas);
    }
}
