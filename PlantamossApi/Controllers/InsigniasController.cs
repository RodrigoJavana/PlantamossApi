using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantamossApi.Data;
using PlantamossApi.DTOs;
using PlantamossApi.Helpers;
using PlantamossApi.Services;

namespace PlantamossApi.Controllers;

/// <summary>Insignias (logros) del usuario autenticado y registro de los eventos que las desbloquean.</summary>
[Authorize]
[Route("api/insignias")]
public class InsigniasController(AppDbContext db, InsigniasService insignias) : ApiControllerBase
{
    // GET /api/insignias   → todas las insignias con su avance, y las desbloqueadas en esta llamada (nuevas)
    [HttpGet]
    public async Task<ActionResult<InsigniasRespuestaDto>> Obtener()
    {
        if (await db.Users.FindAsync(UsuarioId) is null) return Error(StatusCodes.Status401Unauthorized, "Tu sesión ya no es válida.");

        var (todas, nuevas) = await insignias.RevisarAsync(UsuarioId);
        return new InsigniasRespuestaDto { Insignias = todas, Nuevas = nuevas };
    }

    // POST /api/insignias/evento   → registra VER_PLANTA, VER_ESTADO o LEER_BLOG y devuelve las insignias nuevas
    [HttpPost("evento")]
    public async Task<ActionResult<EventoInsigniaRespuestaDto>> RegistrarEvento([FromBody] EventoInsigniaRequest req)
    {
        if (await db.Users.FindAsync(UsuarioId) is null) return Error(StatusCodes.Status401Unauthorized, "Tu sesión ya no es válida.");

        var resultado = await insignias.RegistrarEventoAsync(UsuarioId, req.Evento, req.Referencia);
        if (!resultado.Valido)
        {
            // Evento no permitido o referencia inexistente: 400 con el campo señalado (mismo formato { message, errors } que los 422)
            var errores = new ErroresValidacion();
            errores.Agregar(resultado.Campo!, resultado.Mensaje!);
            return StatusCode(StatusCodes.Status400BadRequest, errores.ComoApiError(resultado.Mensaje!));
        }

        return new EventoInsigniaRespuestaDto { Nuevas = resultado.Nuevas ?? [] };
    }
}
