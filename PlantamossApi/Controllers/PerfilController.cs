using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantamossApi.Data;
using PlantamossApi.DTOs;
using PlantamossApi.Helpers;
using PlantamossApi.Models;
using PlantamossApi.Services;

namespace PlantamossApi.Controllers;

/// <summary>Perfil del usuario autenticado: cambiar nombre, contraseña o eliminar la cuenta.</summary>
[Authorize]
[Route("api/perfil")]
public class PerfilController(AppDbContext db, SesionService sesiones, AuditoriaService auditoria) : ApiControllerBase
{
    // PUT /api/perfil
    [HttpPut]
    public async Task<ActionResult<UserDto>> Actualizar(ActualizarPerfilRequest req)
    {
        var usuario = await db.Users.FindAsync(UsuarioId);
        if (usuario is null) return Error(StatusCodes.Status401Unauthorized, "Tu sesión ya no es válida.");

        usuario.Name = req.Name.Trim();
        usuario.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return usuario.ADto();
    }

    // PUT /api/perfil/password
    [HttpPut("password")]
    public async Task<IActionResult> CambiarPassword(CambiarPasswordRequest req)
    {
        var usuario = await db.Users.FindAsync(UsuarioId);
        if (usuario is null) return Error(StatusCodes.Status401Unauthorized, "Tu sesión ya no es válida.");

        // Fuerza de la nueva contraseña y confirmación: atributos del DTO
        if (!AuthController.VerificarPassword(req.PasswordActual, usuario.Password))
            return Invalido("passwordActual", "La contraseña actual es incorrecta.");

        usuario.Password = BCrypt.Net.BCrypt.HashPassword(req.Password);
        usuario.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        // Cambiar la contraseña cierra TODAS las sesiones (refresh tokens) y, por el claim "stamp",
        // también invalida los tokens de acceso emitidos antes del cambio.
        await sesiones.RevocarTodosAsync(usuario.Id);
        await auditoria.RegistrarAsync(AccionesBitacora.PasswordCambiada, usuario.Id, usuario.Email,
            "Cambio desde el perfil; sesiones revocadas");

        return Ok(new MensajeResponse("Contraseña actualizada correctamente."));
    }

    // GET /api/perfil/ciudades-clima   → ciudades fijadas en el clima de Inicio (o las de por defecto)
    [HttpGet("ciudades-clima")]
    public async Task<ActionResult<List<CiudadClima>>> CiudadesClimaUsuario()
    {
        var usuario = await db.Users.FindAsync(UsuarioId);
        if (usuario is null) return Error(StatusCodes.Status401Unauthorized, "Tu sesión ya no es válida.");

        return Helpers.CiudadesClima.Resolver(usuario.CiudadesClima);
    }

    // PUT /api/perfil/ciudades-clima   body: { ciudades: ["monterrey", ...] }  (1 a 6, en orden)
    [HttpPut("ciudades-clima")]
    public async Task<ActionResult<List<CiudadClima>>> GuardarCiudadesClima(CiudadesClimaRequest req)
    {
        var usuario = await db.Users.FindAsync(UsuarioId);
        if (usuario is null) return Error(StatusCodes.Status401Unauthorized, "Tu sesión ya no es válida.");

        var ids = req.Ciudades.Select(c => c.Trim().ToLowerInvariant()).Distinct().ToList();
        if (ids.Count == 0)
            return Invalido("ciudades", "Selecciona al menos una ciudad.");
        if (ids.Count > Helpers.CiudadesClima.Maximo)
            return Invalido("ciudades", $"Puedes fijar máximo {Helpers.CiudadesClima.Maximo} ciudades.");
        if (ids.FirstOrDefault(id => !Helpers.CiudadesClima.Existe(id)) is { } invalida)
            return Invalido("ciudades", $"La ciudad '{invalida}' no es válida.");

        usuario.CiudadesClima = [.. ids];
        usuario.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Helpers.CiudadesClima.Resolver(ids);
    }

    // DELETE /api/perfil   body: { password }
    [HttpDelete]
    public async Task<IActionResult> Eliminar([FromBody] EliminarCuentaRequest req)
    {
        var usuario = await db.Users.FindAsync(UsuarioId);
        if (usuario is null) return Error(StatusCodes.Status401Unauthorized, "Tu sesión ya no es válida.");

        if (!AuthController.VerificarPassword(req.Password, usuario.Password))
            return Invalido("password", "La contraseña es incorrecta.");

        await sesiones.RevocarTodosAsync(usuario.Id);
        db.Users.Remove(usuario);
        await db.SaveChangesAsync();
        await auditoria.RegistrarAsync(AccionesBitacora.UsuarioEliminado, null, usuario.Email,
            $"Cuenta {usuario.Id} eliminada por el propio usuario");
        return NoContent();
    }
}
