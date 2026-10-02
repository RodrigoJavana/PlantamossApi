using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlantamossApi.Data;
using PlantamossApi.DTOs;
using PlantamossApi.Helpers;
using PlantamossApi.Models;
using PlantamossApi.Services;
using System.ComponentModel.DataAnnotations;

namespace PlantamossApi.Controllers;

/// <summary>
/// Catálogo de flora. Lectura pública; crear/editar/eliminar solo roles editor y admin.
/// </summary>
[Route("api/plantas")]
public class PlantasController(AppDbContext db, AuditoriaService auditoria) : ApiControllerBase
{
    // GET /api/plantas?q=&estado=&clima=&luz=
    [HttpGet]
    public async Task<List<PlantaDto>> Listar(
        [FromQuery, MaxLength(100, ErrorMessage = "La búsqueda no puede superar 100 caracteres.")] string? q,
        [FromQuery] int? estado,
        [FromQuery, MaxLength(20, ErrorMessage = "El clima no es válido.")] string? clima,
        [FromQuery, MaxLength(20, ErrorMessage = "La luz no es válida.")] string? luz)
    {
        var consulta = db.Plantas.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            // Escapamos los comodines de LIKE (%, _ y \) para que se busquen literalmente
            var patron = "%" + q.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";

            // ILIKE = sin distinguir mayúsculas; unaccent() = sin distinguir acentos ("michoacan" encuentra "Michoacán")
            consulta = consulta.Where(p =>
                EF.Functions.ILike(AppDbContext.Unaccent(p.NombrePlanta), AppDbContext.Unaccent(patron))
                || (p.NombreCientifico != null &&
                    EF.Functions.ILike(AppDbContext.Unaccent(p.NombreCientifico), AppDbContext.Unaccent(patron))));
        }

        if (estado is not null) consulta = consulta.Where(p => p.IdEstado == estado);
        if (!string.IsNullOrWhiteSpace(clima)) consulta = consulta.Where(p => p.TipoClima == clima);
        if (!string.IsNullOrWhiteSpace(luz)) consulta = consulta.Where(p => p.LuzSol == luz);

        return await consulta
            .OrderBy(p => p.Estado != null ? p.Estado.NombreEstado : null)
            .ThenBy(p => p.LetraInicial)
            .ThenBy(p => p.NombrePlanta)
            .Select(Mapeos.PlantaProyeccion)
            .ToListAsync();
    }

    // GET /api/plantas/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PlantaDto>> Obtener(int id)
    {
        var planta = await BuscarDto(id);
        return planta is null ? NoEncontrado("Planta no encontrada.") : planta;
    }

    // POST /api/plantas
    [Authorize(Roles = "editor,admin")]
    [HttpPost]
    [ProducesResponseType<PlantaDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Crear(PlantaRequest req)
    {
        if (!await EstadoValido(req.IdEstado))
            return Invalido("idEstado", "El estado seleccionado no existe.");

        var ahora = DateTime.UtcNow;
        var planta = new Planta { CreatedAt = ahora };
        Aplicar(planta, req, ahora);

        db.Plantas.Add(planta);
        await db.SaveChangesAsync();
        await auditoria.RegistrarActorAsync(AccionesBitacora.PlantaCreada, $"Planta {planta.IdPlanta}: {planta.NombrePlanta}");

        return StatusCode(StatusCodes.Status201Created, await BuscarDto(planta.IdPlanta));
    }

    // PUT /api/plantas/{id}
    [Authorize(Roles = "editor,admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, PlantaRequest req)
    {
        var planta = await db.Plantas.FindAsync(id);
        if (planta is null) return NoEncontrado("Planta no encontrada.");

        if (!await EstadoValido(req.IdEstado))
            return Invalido("idEstado", "El estado seleccionado no existe.");

        Aplicar(planta, req, DateTime.UtcNow);
        await db.SaveChangesAsync();
        await auditoria.RegistrarActorAsync(AccionesBitacora.PlantaEditada, $"Planta {id}: {planta.NombrePlanta}");

        return Ok(await BuscarDto(id));
    }

    // DELETE /api/plantas/{id}
    [Authorize(Roles = "editor,admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var borradas = await db.Plantas.Where(p => p.IdPlanta == id).ExecuteDeleteAsync();
        if (borradas == 0) return NoEncontrado("Planta no encontrada.");
        await auditoria.RegistrarActorAsync(AccionesBitacora.PlantaEliminada, $"Planta {id}");
        return NoContent();
    }

    // ----------------- utilidades privadas -----------------

    private Task<PlantaDto?> BuscarDto(int id) =>
        db.Plantas.Where(p => p.IdPlanta == id).Select(Mapeos.PlantaProyeccion).FirstOrDefaultAsync();

    /// <summary>
    /// Única regla que necesita la BD: el estado debe existir (o venir vacío).
    /// Clima, luz y URL ya los validaron los atributos de <see cref="PlantaRequest"/>.
    /// </summary>
    private async Task<bool> EstadoValido(int? idEstado) =>
        idEstado is null || await db.Estados.AnyAsync(e => e.IdEstado == idEstado);

    /// <summary>Copia los datos del request a la entidad.</summary>
    private static void Aplicar(Planta planta, PlantaRequest req, DateTime ahora)
    {
        planta.NombrePlanta = req.NombrePlanta.Trim();
        planta.LetraInicial = Validaciones.LetraInicial(planta.NombrePlanta);
        planta.NombreCientifico = Validaciones.VacioANull(req.NombreCientifico);
        planta.IdEstado = req.IdEstado;
        planta.UrlImagen = Validaciones.VacioANull(req.UrlImagen);
        planta.TipoClima = Validaciones.VacioANull(req.TipoClima) ?? TiposClima.Templado;
        planta.LuzSol = Validaciones.VacioANull(req.LuzSol) ?? TiposLuz.SolDirecto;
        planta.Riego = Validaciones.VacioANull(req.Riego);
        planta.Descripcion = Validaciones.VacioANull(req.Descripcion);
        planta.CuidadosTexto = Validaciones.VacioANull(req.CuidadosTexto);
        planta.UpdatedAt = ahora;
    }
}
