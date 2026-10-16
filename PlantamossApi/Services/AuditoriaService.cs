using Microsoft.EntityFrameworkCore;
using PlantamossApi.Data;
using PlantamossApi.Helpers;
using PlantamossApi.Models;

namespace PlantamossApi.Services;

/// <summary>
/// Bitácora de auditoría (CONTRATO 5.2): registra eventos de seguridad y de administración
/// en la tabla <c>bitacora_auditoria</c> y, además, en el log de Serilog.
/// <para>
/// Usa su PROPIO DbContext para que guardar la bitácora nunca arrastre (ni dependa de) los cambios
/// pendientes del controlador. Si la BD falla, el error se registra en el log y la petición continúa.
/// </para>
/// NUNCA se guardan contraseñas, códigos ni tokens en <c>detalle</c>.
/// </summary>
public class AuditoriaService(
    DbContextOptions<AppDbContext> opcionesDb,
    IHttpContextAccessor httpAccessor,
    ILogger<AuditoriaService> logger)
{
    /// <summary>Registra un evento indicando explícitamente el usuario afectado.</summary>
    public async Task RegistrarAsync(string accion, long? userId, string? email, string? detalle = null, bool exito = true)
    {
        var http = httpAccessor.HttpContext;
        var registro = new BitacoraAuditoria
        {
            Fecha = DateTime.UtcNow,
            UserId = userId,
            Email = Recortar(email, 255),
            Accion = Recortar(accion, 60)!,
            Detalle = Recortar(detalle, 500),
            Ip = ClienteHttp.Ip(http),
            Exito = exito,
        };

        // También al log (con nivel según el resultado) para correlacionarlo con el TraceId de la petición
        logger.Log(exito ? LogLevel.Information : LogLevel.Warning,
            "Auditoria {Accion} usuario={UserId} email={Email} exito={Exito} detalle={Detalle}",
            registro.Accion, registro.UserId, registro.Email, registro.Exito, registro.Detalle);

        try
        {
            await using var db = new AppDbContext(opcionesDb);
            db.Bitacora.Add(registro);
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo guardar el evento de auditoría {Accion}", registro.Accion);
        }
    }

    /// <summary>Registra un evento hecho por el usuario autenticado (toma sub y email del JWT).</summary>
    public Task RegistrarActorAsync(string accion, string? detalle = null, bool exito = true)
    {
        var usuario = httpAccessor.HttpContext?.User;
        long? id = long.TryParse(usuario?.FindFirst("sub")?.Value, out var sub) ? sub : null;
        return RegistrarAsync(accion, id, usuario?.FindFirst("email")?.Value, detalle, exito);
    }

    private static string? Recortar(string? texto, int max) =>
        texto is null ? null : texto.Length > max ? texto[..max] : texto;
}
