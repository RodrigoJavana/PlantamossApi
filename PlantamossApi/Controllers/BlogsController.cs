using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlantamossApi.Data;
using PlantamossApi.DTOs;
using PlantamossApi.Helpers;
using PlantamossApi.Models;
using PlantamossApi.Services;

namespace PlantamossApi.Controllers;

/// <summary>
/// Mini blog de la comunidad.
/// Lectura pública; publicar requiere sesión y correo verificado;
/// editar/eliminar solo el dueño o los roles noticias/admin (moderación).
/// </summary>
[Route("api/blogs")]
public class BlogsController(AppDbContext db, AuditoriaService auditoria) : ApiControllerBase
{
    // GET /api/blogs
    [HttpGet]
    public async Task<List<BlogDto>> Listar() =>
        await db.Miniblogs
            .OrderByDescending(b => b.FechaPublicacion)
            .Select(Mapeos.BlogProyeccion)
            .ToListAsync();

    // GET /api/blogs/mios
    [Authorize]
    [HttpGet("mios")]
    public async Task<List<BlogDto>> Mios()
    {
        var id = UsuarioId;
        return await db.Miniblogs
            .Where(b => b.IdUsuario == id)
            .OrderByDescending(b => b.FechaPublicacion)
            .Select(Mapeos.BlogProyeccion)
            .ToListAsync();
    }

    // GET /api/blogs/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<BlogDto>> Obtener(int id)
    {
        var blog = await BuscarDto(id);
        return blog is null ? NoEncontrado("Blog no encontrado.") : blog;
    }

    // POST /api/blogs
    [Authorize]
    [HttpPost]
    [ProducesResponseType<BlogDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Crear(BlogRequest req)
    {
        var usuario = await db.Users.FindAsync(UsuarioId);
        if (usuario is null) return Error(StatusCodes.Status401Unauthorized, "Tu sesión ya no es válida.");
        if (usuario.EmailVerifiedAt is null) return Prohibido("Verifica tu correo para publicar.");

        var blog = new Miniblog { IdUsuario = usuario.Id, FechaPublicacion = DateTime.UtcNow };
        Aplicar(blog, req);

        db.Miniblogs.Add(blog);
        await db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, await BuscarDto(blog.IdBlog));
    }

    // PUT /api/blogs/{id}
    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, BlogRequest req)
    {
        var blog = await db.Miniblogs.FindAsync(id);
        if (blog is null) return NoEncontrado("Blog no encontrado.");
        if (!PuedeModificar(blog)) return Prohibido("No tienes permiso para editar este blog.");

        Aplicar(blog, req);
        await db.SaveChangesAsync();

        return Ok(await BuscarDto(id));
    }

    // DELETE /api/blogs/{id}
    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var blog = await db.Miniblogs.FindAsync(id);
        if (blog is null) return NoEncontrado("Blog no encontrado.");
        if (!PuedeModificar(blog)) return Prohibido("No tienes permiso para eliminar este blog.");

        db.Miniblogs.Remove(blog);
        await db.SaveChangesAsync();
        await auditoria.RegistrarActorAsync(AccionesBitacora.BlogEliminado,
            $"Blog {id}: {blog.Titulo}" + (blog.IdUsuario == UsuarioId ? "" : " (moderación)"));
        return NoContent();
    }

    // ----------------- utilidades privadas -----------------

    /// <summary>El dueño del blog o un moderador (noticias/admin).</summary>
    private bool PuedeModificar(Miniblog blog) =>
        blog.IdUsuario == UsuarioId || User.IsInRole(Roles.Noticias) || User.IsInRole(Roles.Admin);

    private Task<BlogDto?> BuscarDto(int id) =>
        db.Miniblogs.Where(b => b.IdBlog == id).Select(Mapeos.BlogProyeccion).FirstOrDefaultAsync();

    private static void Aplicar(Miniblog blog, BlogRequest req)
    {
        blog.Titulo = req.Titulo.Trim();
        blog.Contenido = req.Contenido.Trim();
        blog.Contenido2 = Validaciones.VacioANull(req.Contenido2);
        blog.Contenido3 = Validaciones.VacioANull(req.Contenido3);
        blog.UrlImagen = Validaciones.VacioANull(req.UrlImagen);
        blog.UrlImagen2 = Validaciones.VacioANull(req.UrlImagen2);
        blog.UrlImagen3 = Validaciones.VacioANull(req.UrlImagen3);
        blog.UrlImagen4 = Validaciones.VacioANull(req.UrlImagen4);
        blog.UrlImagen5 = Validaciones.VacioANull(req.UrlImagen5);
    }
}
