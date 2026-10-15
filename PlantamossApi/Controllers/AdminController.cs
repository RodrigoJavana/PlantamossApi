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
/// Administración de usuarios (solo admin) y estadísticas del panel (admin, editor y noticias).
/// </summary>
[Authorize(Roles = "admin")]
[Route("api/admin")]
public class AdminController(AppDbContext db, SesionService sesiones, AuditoriaService auditoria) : ApiControllerBase
{
    // GET /api/admin/usuarios?q=
    [HttpGet("usuarios")]
    public async Task<List<UserDto>> Listar([FromQuery, MaxLength(100, ErrorMessage = "La búsqueda no puede superar 100 caracteres.")] string? q)
    {
        var consulta = db.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var patron = "%" + q.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            consulta = consulta.Where(u => EF.Functions.ILike(u.Name, patron) || EF.Functions.ILike(u.Email, patron));
        }

        return await consulta
            .OrderByDescending(u => u.CreatedAt)
            .Select(Mapeos.UserProyeccion)
            .ToListAsync();
    }

    // GET /api/admin/usuarios/{id}
    [HttpGet("usuarios/{id:long}")]
    public async Task<ActionResult<UserDto>> Obtener(long id)
    {
        var usuario = await db.Users.FindAsync(id);
        return usuario is null ? NoEncontrado("Usuario no encontrado.") : usuario.ADto();
    }

    // POST /api/admin/usuarios
    [HttpPost("usuarios")]
    [ProducesResponseType<UserDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Crear(CrearUsuarioRequest req)
    {
        var correo = Validaciones.NormalizarEmail(req.Email);
        // Rol, estado y longitud de la contraseña ya los validaron los atributos del DTO
        if (await db.Users.AnyAsync(u => u.Email == correo))
            return Invalido("email", "Este correo ya está registrado.");

        var ahora = DateTime.UtcNow;
        var usuario = new User
        {
            Name = req.Name,
            Email = correo,
            Role = req.Role,
            Estado = req.Estado,
            Password = BCrypt.Net.BCrypt.HashPassword(req.Password),
            EmailVerifiedAt = ahora, // creado por el admin → correo ya verificado
            CreatedAt = ahora,
            UpdatedAt = ahora,
        };
        db.Users.Add(usuario);
        await db.SaveChangesAsync();

        await auditoria.RegistrarActorAsync(AccionesBitacora.UsuarioCreado,
            $"Creó la cuenta {usuario.Id} ({usuario.Email}) con rol {usuario.Role} y estado {usuario.Estado}");
        return StatusCode(StatusCodes.Status201Created, usuario.ADto());
    }

    // PUT /api/admin/usuarios/{id}
    [HttpPut("usuarios/{id:long}")]
    public async Task<IActionResult> Actualizar(long id, EditarUsuarioRequest req)
    {
        var usuario = await db.Users.FindAsync(id);
        if (usuario is null) return NoEncontrado("Usuario no encontrado.");

        // Evitar que el admin se quede fuera del sistema por accidente
        if (id == UsuarioId && (req.Role != Roles.Admin || req.Estado != EstadosCuenta.Activo))
            return ReglaNegocio("No puedes quitarte el rol de administrador ni suspender tu propia cuenta.");

        var cambios = new List<string>();
        if (usuario.Role != req.Role) cambios.Add($"rol {usuario.Role}->{req.Role}");
        if (usuario.Estado != req.Estado) cambios.Add($"estado {usuario.Estado}->{req.Estado}");
        if (usuario.Name != req.Name) cambios.Add("nombre");
        var cambiaPassword = !string.IsNullOrEmpty(req.Password);
        if (cambiaPassword) cambios.Add("contraseña");

        usuario.Name = req.Name;
        usuario.Role = req.Role;
        usuario.Estado = req.Estado;
        if (cambiaPassword)
            usuario.Password = BCrypt.Net.BCrypt.HashPassword(req.Password);
        usuario.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        // Suspender la cuenta o cambiarle la contraseña cierra todas sus sesiones
        if (cambiaPassword || usuario.Estado == EstadosCuenta.Suspendido)
            await sesiones.RevocarTodosAsync(usuario.Id);

        await auditoria.RegistrarActorAsync(AccionesBitacora.UsuarioEditado,
            $"Editó la cuenta {usuario.Id} ({usuario.Email}): {(cambios.Count == 0 ? "sin cambios" : string.Join(", ", cambios))}");
        return Ok(usuario.ADto());
    }

    // DELETE /api/admin/usuarios/{id}
    [HttpDelete("usuarios/{id:long}")]
    public async Task<IActionResult> Eliminar(long id)
    {
        if (id == UsuarioId) return ReglaNegocio("No puedes eliminar tu propia cuenta desde el panel.");

        var usuario = await db.Users.FindAsync(id);
        if (usuario is null) return NoEncontrado("Usuario no encontrado.");

        await sesiones.RevocarTodosAsync(usuario.Id);
        db.Users.Remove(usuario);
        await db.SaveChangesAsync();

        await auditoria.RegistrarActorAsync(AccionesBitacora.UsuarioEliminado,
            $"Eliminó la cuenta {id} ({usuario.Email})");
        return NoContent();
    }

    // GET /api/admin/bitacora?accion=&email=&desde=&hasta=&pagina=1&tamano=50   (CONTRATO 5.2)
    [HttpGet("bitacora")]
    public async Task<ActionResult<PaginaDto<BitacoraDto>>> Bitacora(
        [FromQuery, MaxLength(60, ErrorMessage = "La acción no puede superar 60 caracteres.")] string? accion,
        [FromQuery, MaxLength(255, ErrorMessage = "El correo no puede superar 255 caracteres.")] string? email,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery, Range(1, 100000, ErrorMessage = "La página debe ser un número mayor o igual a 1.")] int pagina = 1,
        [FromQuery, Range(1, 200, ErrorMessage = "El tamaño de página debe estar entre 1 y 200.")] int tamano = 50)
    {
        var consulta = db.Bitacora.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(accion))
            consulta = consulta.Where(b => b.Accion == accion.Trim());
        if (!string.IsNullOrWhiteSpace(email))
        {
            var patron = "%" + email.Trim().ToLowerInvariant().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            consulta = consulta.Where(b => b.Email != null && EF.Functions.ILike(b.Email, patron));
        }
        if (desde is not null)
        {
            var d = DateTime.SpecifyKind(desde.Value, desde.Value.Kind == DateTimeKind.Unspecified ? DateTimeKind.Utc : desde.Value.Kind).ToUniversalTime();
            consulta = consulta.Where(b => b.Fecha >= d);
        }
        if (hasta is not null)
        {
            var h = DateTime.SpecifyKind(hasta.Value, hasta.Value.Kind == DateTimeKind.Unspecified ? DateTimeKind.Utc : hasta.Value.Kind).ToUniversalTime();
            // Si "hasta" es solo una fecha (sin hora) se incluye el día completo
            if (h.TimeOfDay == TimeSpan.Zero) h = h.AddDays(1).AddTicks(-1);
            consulta = consulta.Where(b => b.Fecha <= h);
        }

        var total = await consulta.CountAsync();
        var items = await consulta
            .OrderByDescending(b => b.Fecha).ThenByDescending(b => b.Id)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .Select(b => new BitacoraDto(b.Id, b.Fecha, b.UserId, b.Email, b.Accion, b.Detalle, b.Ip, b.Exito))
            .ToListAsync();

        return new PaginaDto<BitacoraDto>(total, pagina, tamano, items);
    }
}

/// <summary>
/// Estadísticas para el dashboard del panel. Las pueden ver admin, editor y noticias (CONTRATO v2).
/// Va en un controlador aparte porque el [Authorize(Roles = "admin")] de AdminController
/// se sumaría (AND) a cualquier otro atributo puesto en la acción.
/// </summary>
[Authorize(Roles = "admin,editor,noticias")]
[Route("api/admin")]
public class EstadisticasController(AppDbContext db) : ApiControllerBase
{
    // GET /api/admin/estadisticas  (también para editor y noticias: dashboard del panel)
    [HttpGet("estadisticas")]
    public async Task<EstadisticasDto> Estadisticas()
    {
        // Conteo por rol en una sola consulta (GROUP BY role)
        var porRol = await db.Users
            .GroupBy(u => u.Role)
            .Select(g => new { Rol = g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.Rol, x => x.Total);

        int Rol(string r) => porRol.GetValueOrDefault(r);

        return new EstadisticasDto(
            TotalUsuarios: porRol.Values.Sum(),
            UsuariosActivos: await db.Users.CountAsync(u => u.Estado == EstadosCuenta.Activo),
            UsuariosSuspendidos: await db.Users.CountAsync(u => u.Estado == EstadosCuenta.Suspendido),
            TotalPlantas: await db.Plantas.CountAsync(),
            TotalNoticias: await db.Noticias.CountAsync(),
            TotalBlogs: await db.Miniblogs.CountAsync(),
            UsuariosPorRol: new UsuariosPorRolDto(Rol(Roles.User), Rol(Roles.Editor), Rol(Roles.Noticias), Rol(Roles.Admin)));
    }
}
