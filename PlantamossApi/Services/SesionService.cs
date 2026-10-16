using Microsoft.EntityFrameworkCore;
using PlantamossApi.Data;
using PlantamossApi.DTOs;
using PlantamossApi.Helpers;
using PlantamossApi.Models;

namespace PlantamossApi.Services;

/// <summary>Resultado de intentar renovar la sesión con un refresh token.</summary>
public enum ResultadoRefresh
{
    Ok,
    /// <summary>No existe, expiró o el usuario ya no puede entrar.</summary>
    Invalido,
    /// <summary>El token ya había sido usado/revocado: posible robo → se revocan TODAS las sesiones del usuario.</summary>
    Reuso,
    /// <summary>La cuenta está suspendida.</summary>
    Suspendido,
}

/// <summary>
/// Sesiones con refresh token (CONTRATO 5.1):
/// <list type="bullet">
/// <item>Emite el par token de acceso (JWT corto) + refresh token (opaco, 7 días).</item>
/// <item>En la BD solo se guarda el SHA-256 del refresh token.</item>
/// <item>ROTACIÓN: cada refresh usado se revoca y se entrega uno nuevo (<c>reemplazado_por</c>).</item>
/// <item>DETECCIÓN DE REUSO: si llega un refresh ya revocado se revocan todos los del usuario.</item>
/// </list>
/// </summary>
public class SesionService(
    AppDbContext db,
    TokenService tokens,
    SeguridadOptions seguridad,
    IHttpContextAccessor httpAccessor)
{
    /// <summary>Crea una sesión nueva (login / registro) y devuelve la respuesta completa.</summary>
    public async Task<AuthResponse> CrearSesionAsync(User usuario)
    {
        var ahora = DateTime.UtcNow;

        // Limpieza: los refresh ya expirados de este usuario no sirven para nada
        await db.RefreshTokens.Where(r => r.UserId == usuario.Id && r.ExpiraEn < ahora).ExecuteDeleteAsync();

        var refresh = await GuardarNuevoAsync(usuario.Id, ahora);
        return Respuesta(usuario, refresh);
    }

    /// <summary>Rota el refresh token: revoca el usado y entrega un par nuevo.</summary>
    public async Task<(ResultadoRefresh Resultado, AuthResponse? Respuesta, User? Usuario)> RotarAsync(string refreshToken)
    {
        var hash = TokenService.HashRefreshToken(refreshToken);
        var ahora = DateTime.UtcNow;

        var registro = await db.RefreshTokens.AsNoTracking().FirstOrDefaultAsync(r => r.TokenHash == hash);
        if (registro is null) return (ResultadoRefresh.Invalido, null, null);

        var usuario = await db.Users.FindAsync(registro.UserId);
        if (usuario is null) return (ResultadoRefresh.Invalido, null, null);

        // Reuso de un token ya revocado → alguien más lo tiene: cerramos TODAS las sesiones
        if (registro.RevocadoEn is not null)
        {
            await RevocarTodosAsync(usuario.Id);
            return (ResultadoRefresh.Reuso, null, usuario);
        }

        if (registro.ExpiraEn <= ahora) return (ResultadoRefresh.Invalido, null, usuario);

        if (usuario.Estado == EstadosCuenta.Suspendido)
        {
            await RevocarTodosAsync(usuario.Id);
            return (ResultadoRefresh.Suspendido, null, usuario);
        }

        // Revocación ATÓMICA: si dos peticiones usan el mismo refresh a la vez, solo una gana;
        // la otra cuenta como reuso.
        var nuevo = TokenService.GenerarRefreshToken();
        var nuevoHash = TokenService.HashRefreshToken(nuevo);
        var revocados = await db.RefreshTokens
            .Where(r => r.Id == registro.Id && r.RevocadoEn == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.RevocadoEn, ahora)
                .SetProperty(r => r.ReemplazadoPor, nuevoHash));

        if (revocados == 0)
        {
            await RevocarTodosAsync(usuario.Id);
            return (ResultadoRefresh.Reuso, null, usuario);
        }

        await GuardarAsync(usuario.Id, nuevoHash, ahora);
        return (ResultadoRefresh.Ok, Respuesta(usuario, nuevo), usuario);
    }

    /// <summary>Revoca un refresh token concreto (logout). Devuelve el usuario dueño, si existía.</summary>
    public async Task<long?> RevocarAsync(string refreshToken)
    {
        var hash = TokenService.HashRefreshToken(refreshToken);
        var registro = await db.RefreshTokens.AsNoTracking()
            .Where(r => r.TokenHash == hash)
            .Select(r => new { r.Id, r.UserId })
            .FirstOrDefaultAsync();
        if (registro is null) return null;

        await db.RefreshTokens
            .Where(r => r.Id == registro.Id && r.RevocadoEn == null)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.RevocadoEn, DateTime.UtcNow));
        return registro.UserId;
    }

    /// <summary>Revoca TODOS los refresh vigentes del usuario (logout-todo, cambio de contraseña, suspensión...).</summary>
    public Task<int> RevocarTodosAsync(long userId) =>
        db.RefreshTokens
            .Where(r => r.UserId == userId && r.RevocadoEn == null)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.RevocadoEn, DateTime.UtcNow));

    // ----------------- utilidades privadas -----------------

    private async Task<string> GuardarNuevoAsync(long userId, DateTime ahora)
    {
        var token = TokenService.GenerarRefreshToken();
        await GuardarAsync(userId, TokenService.HashRefreshToken(token), ahora);
        return token;
    }

    private async Task GuardarAsync(long userId, string hash, DateTime ahora)
    {
        var http = httpAccessor.HttpContext;
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = userId,
            TokenHash = hash,
            CreadoEn = ahora,
            ExpiraEn = ahora.AddDays(Math.Max(1, seguridad.DiasRefreshToken)),
            Ip = ClienteHttp.Ip(http),
            UserAgent = ClienteHttp.UserAgent(http),
        });
        await db.SaveChangesAsync();
    }

    private AuthResponse Respuesta(User usuario, string refresh) =>
        new(tokens.CrearTokenSesion(usuario), refresh, tokens.SegundosVidaAcceso, usuario.ADto());
}
