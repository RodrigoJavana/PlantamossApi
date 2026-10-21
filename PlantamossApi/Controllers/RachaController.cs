using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantamossApi.Data;
using PlantamossApi.DTOs;
using PlantamossApi.Services;

namespace PlantamossApi.Controllers;

/// <summary>Racha de días seguidos del usuario autenticado y el crecimiento de su planta.</summary>
[Authorize]
[Route("api/racha")]
public class RachaController(AppDbContext db, RachaService racha) : ApiControllerBase
{
    // POST /api/racha/registrar   → registra la visita de hoy (idempotente en el mismo día)
    [HttpPost("registrar")]
    public async Task<ActionResult<RachaDto>> Registrar()
    {
        if (await db.Users.FindAsync(UsuarioId) is null) return Error(StatusCodes.Status401Unauthorized, "Tu sesión ya no es válida.");

        return await racha.RegistrarAsync(UsuarioId);
    }

    // GET /api/racha   → estado actual sin modificar nada
    [HttpGet]
    public async Task<ActionResult<RachaDto>> Obtener()
    {
        if (await db.Users.FindAsync(UsuarioId) is null) return Error(StatusCodes.Status401Unauthorized, "Tu sesión ya no es válida.");

        return await racha.ObtenerAsync(UsuarioId);
    }
}
