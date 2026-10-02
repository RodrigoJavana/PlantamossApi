using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlantamossApi.Data;
using PlantamossApi.DTOs;
using PlantamossApi.Helpers;
using PlantamossApi.Models;
using PlantamossApi.Services;

namespace PlantamossApi.Controllers;

/// <summary>Noticias públicas para la página de inicio.</summary>
[Route("api/noticias")]
public class NoticiasController(AppDbContext db) : ApiControllerBase
{
    // GET /api/noticias/recientes  → máximo 4 activas, destacadas primero
    [HttpGet("recientes")]
    public async Task<List<NoticiaDto>> Recientes() =>
        await db.Noticias
            .Where(n => n.Activa)
            .OrderByDescending(n => n.Destacada)
            .ThenByDescending(n => n.FechaPublicacion)
            .Take(4)
            .Select(Mapeos.NoticiaProyeccion)
            .ToListAsync();
}

/// <summary>Panel del editor de noticias (roles noticias y admin).</summary>
[Authorize(Roles = "noticias,admin")]
[Route("api/editor/noticias")]
public class EditorNoticiasController(AppDbContext db, AuditoriaService auditoria) : ApiControllerBase
{
    // GET /api/editor/noticias
    [HttpGet]
    public async Task<List<NoticiaDto>> Listar() =>
        await db.Noticias
            .OrderByDescending(n => n.FechaPublicacion)
            .Select(Mapeos.NoticiaProyeccion)
            .ToListAsync();

    // POST /api/editor/noticias
    [HttpPost]
    [ProducesResponseType<NoticiaDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Crear(NoticiaRequest req)
    {
        var noticia = new Noticia
        {
            IdEditor = UsuarioId,
            Activa = true,
            FechaPublicacion = DateTime.UtcNow,
        };
        Aplicar(noticia, req);

        db.Noticias.Add(noticia);
        await db.SaveChangesAsync();
        await auditoria.RegistrarActorAsync(AccionesBitacora.NoticiaCreada, $"Noticia {noticia.IdNoticia}: {noticia.Titulo}");

        return StatusCode(StatusCodes.Status201Created, await BuscarDto(noticia.IdNoticia));
    }

    // PUT /api/editor/noticias/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, NoticiaRequest req)
    {
        var noticia = await db.Noticias.FindAsync(id);
        if (noticia is null) return NoEncontrado("Noticia no encontrada.");

        Aplicar(noticia, req);
        await db.SaveChangesAsync();
        await auditoria.RegistrarActorAsync(AccionesBitacora.NoticiaEditada, $"Noticia {id}: {noticia.Titulo}");

        return Ok(await BuscarDto(id));
    }

    // PATCH /api/editor/noticias/{id}/toggle  → invierte "activa"
    [HttpPatch("{id:int}/toggle")]
    public async Task<IActionResult> Toggle(int id)
    {
        var noticia = await db.Noticias.FindAsync(id);
        if (noticia is null) return NoEncontrado("Noticia no encontrada.");

        noticia.Activa = !noticia.Activa;
        await db.SaveChangesAsync();
        await auditoria.RegistrarActorAsync(
            noticia.Activa ? AccionesBitacora.NoticiaActivada : AccionesBitacora.NoticiaDesactivada, $"Noticia {id}");

        return Ok(await BuscarDto(id));
    }

    // DELETE /api/editor/noticias/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var borradas = await db.Noticias.Where(n => n.IdNoticia == id).ExecuteDeleteAsync();
        if (borradas == 0) return NoEncontrado("Noticia no encontrada.");
        await auditoria.RegistrarActorAsync(AccionesBitacora.NoticiaEliminada, $"Noticia {id}");
        return NoContent();
    }

    // ----------------- utilidades privadas -----------------

    private Task<NoticiaDto?> BuscarDto(int id) =>
        db.Noticias.Where(n => n.IdNoticia == id).Select(Mapeos.NoticiaProyeccion).FirstOrDefaultAsync();

    private static void Aplicar(Noticia noticia, NoticiaRequest req)
    {
        noticia.Titulo = req.Titulo.Trim();
        noticia.Contenido = req.Contenido.Trim();
        noticia.UrlImagen = Validaciones.VacioANull(req.UrlImagen);
        noticia.UrlExterna = Validaciones.VacioANull(req.UrlExterna);
        noticia.Destacada = req.Destacada;
    }
}
