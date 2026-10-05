using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using PlantamossApi.Data;
using PlantamossApi.DTOs;
using PlantamossApi.Helpers;
using PlantamossApi.Models;
using PlantamossApi.Services;

namespace PlantamossApi.Controllers;

/// <summary>
/// Registro, login, sesiones con refresh token (CONTRATO 5.1), verificación de correo
/// y recuperación de contraseña con código de 6 dígitos.
/// </summary>
[Route("api/auth")]
public class AuthController(
    AppDbContext db,
    TokenService tokens,
    SesionService sesiones,
    AuditoriaService auditoria,
    CodigoService codigos,
    EmailService email,
    LoginIntentosService intentos) : ApiControllerBase
{
    // ---------------------------------------------------------------
    // POST /api/auth/register
    // ---------------------------------------------------------------
    [HttpPost("register")]
    [EnableRateLimiting("registro")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        var correo = Validaciones.NormalizarEmail(req.Email);
        // El formato (password fuerte, confirmación, términos...) ya lo validaron los atributos del DTO.
        // Aquí solo queda la regla que necesita la BD:
        if (await db.Users.AnyAsync(u => u.Email == correo))
            return Invalido("email", "Este correo ya está registrado.");

        var ahora = DateTime.UtcNow;
        var usuario = new User
        {
            Name = req.Name.Trim(),
            Email = correo,
            Password = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Role = Roles.User,
            Estado = EstadosCuenta.Activo,
            CreatedAt = ahora,
            UpdatedAt = ahora,
        };
        db.Users.Add(usuario);
        await db.SaveChangesAsync();

        // Enviar código de verificación (si el correo falla, el usuario puede pedir reenvío)
        var codigo = await codigos.GenerarAsync(correo, TiposCodigo.Verificacion);
        await email.EnviarCodigoAsync(correo, codigo, TiposCodigo.Verificacion);

        await auditoria.RegistrarAsync(AccionesBitacora.Registro, usuario.Id, usuario.Email);
        return StatusCode(StatusCodes.Status201Created, await sesiones.CrearSesionAsync(usuario));
    }

    // ---------------------------------------------------------------
    // POST /api/auth/login
    // ---------------------------------------------------------------
    [HttpPost("login")]
    [EnableRateLimiting("login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var correo = Validaciones.NormalizarEmail(req.Email);

        // Bloqueo temporal de la cuenta tras varios intentos fallidos (aunque ahora la contraseña sea correcta)
        if (intentos.MinutosRestantes(correo) is { } minutos)
        {
            await auditoria.RegistrarAsync(AccionesBitacora.LoginBloqueado, null, correo,
                "Intento durante el bloqueo temporal", exito: false);
            return Error(StatusCodes.Status429TooManyRequests,
                $"Demasiados intentos fallidos. La cuenta está bloqueada temporalmente; inténtalo de nuevo en {minutos} min.");
        }

        var usuario = await db.Users.FirstOrDefaultAsync(u => u.Email == correo);
        if (usuario is null || !VerificarPassword(req.Password, usuario.Password))
        {
            var bloqueada = intentos.RegistrarFallo(correo);
            await auditoria.RegistrarAsync(AccionesBitacora.LoginFallido, usuario?.Id, correo,
                usuario is null ? "Correo no registrado" : "Contraseña incorrecta", exito: false);
            if (bloqueada)
                await auditoria.RegistrarAsync(AccionesBitacora.LoginBloqueado, usuario?.Id, correo,
                    $"Cuenta bloqueada temporalmente tras {intentos.MaxFallos} intentos fallidos", exito: false);
            return Error(StatusCodes.Status401Unauthorized, "Credenciales incorrectas.");
        }

        if (usuario.Estado == EstadosCuenta.Suspendido)
        {
            await auditoria.RegistrarAsync(AccionesBitacora.LoginFallido, usuario.Id, correo, "Cuenta suspendida", exito: false);
            return Prohibido("Tu cuenta está suspendida. Contacta al administrador.");
        }

        intentos.Limpiar(correo);
        var respuesta = await sesiones.CrearSesionAsync(usuario);
        await auditoria.RegistrarAsync(AccionesBitacora.LoginOk, usuario.Id, correo);
        return Ok(respuesta);
    }

    // ---------------------------------------------------------------
    // POST /api/auth/refresh   { refreshToken }  → par nuevo (ROTACIÓN)
    // ---------------------------------------------------------------
    [HttpPost("refresh")]
    [EnableRateLimiting("refresh")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Refresh(RefreshRequest req)
    {
        var (resultado, respuesta, usuario) = await sesiones.RotarAsync(req.RefreshToken);
        switch (resultado)
        {
            case ResultadoRefresh.Ok:
                return Ok(respuesta);
            case ResultadoRefresh.Reuso:
                // Un refresh revocado volvió a usarse: posible robo. Ya se revocaron TODAS sus sesiones.
                await auditoria.RegistrarAsync(AccionesBitacora.RefreshReuso, usuario?.Id, usuario?.Email,
                    "Refresh token reutilizado: se revocaron todas las sesiones", exito: false);
                return Error(StatusCodes.Status401Unauthorized, "Tu sesión ya no es válida. Inicia sesión de nuevo.");
            case ResultadoRefresh.Suspendido:
                return Error(StatusCodes.Status401Unauthorized, "Tu cuenta está suspendida. Contacta al administrador.");
            default:
                return Error(StatusCodes.Status401Unauthorized, "Tu sesión expiró. Inicia sesión de nuevo.");
        }
    }

    // ---------------------------------------------------------------
    // POST /api/auth/logout   { refreshToken }  (auth opcional) → 204
    // ---------------------------------------------------------------
    [HttpPost("logout")]
    [EnableRateLimiting("refresh")]
    public async Task<IActionResult> Logout([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] LogoutRequest? req)
    {
        long? userId = null;
        if (!string.IsNullOrWhiteSpace(req?.RefreshToken))
            userId = await sesiones.RevocarAsync(req.RefreshToken);

        if (userId is not null)
        {
            var correo = await db.Users.Where(u => u.Id == userId).Select(u => u.Email).FirstOrDefaultAsync();
            await auditoria.RegistrarAsync(AccionesBitacora.Logout, userId, correo, "Cierre de sesión");
        }
        return NoContent();
    }

    // ---------------------------------------------------------------
    // POST /api/auth/logout-todo  (auth) → 204: cierra la sesión en TODOS los dispositivos
    // ---------------------------------------------------------------
    [Authorize]
    [HttpPost("logout-todo")]
    public async Task<IActionResult> LogoutTodo()
    {
        var revocados = await sesiones.RevocarTodosAsync(UsuarioId);
        await auditoria.RegistrarActorAsync(AccionesBitacora.Logout, $"Cierre de todas las sesiones ({revocados})");
        return NoContent();
    }

    // ---------------------------------------------------------------
    // GET /api/auth/me
    // ---------------------------------------------------------------
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me()
    {
        var usuario = await db.Users.FindAsync(UsuarioId);
        if (usuario is null) return Error(StatusCodes.Status401Unauthorized, "Tu sesión ya no es válida.");
        return usuario.ADto();
    }

    // ---------------------------------------------------------------
    // POST /api/auth/verificar-correo
    // ---------------------------------------------------------------
    [Authorize]
    [HttpPost("verificar-correo")]
    [EnableRateLimiting("verificar-codigo")]
    public async Task<IActionResult> VerificarCorreo(VerificarCorreoRequest req)
    {
        var usuario = await db.Users.FindAsync(UsuarioId);
        if (usuario is null) return Error(StatusCodes.Status401Unauthorized, "Tu sesión ya no es válida.");

        if (usuario.EmailVerifiedAt is not null)
            return Ok(new MensajeUsuarioResponse("Tu correo ya estaba verificado.", usuario.ADto()));

        var resultado = await codigos.VerificarAsync(usuario.Email, TiposCodigo.Verificacion, req.Codigo);
        if (resultado != ResultadoCodigo.Valido)
            return Invalido("codigo", CodigoService.Mensaje(resultado));

        usuario.EmailVerifiedAt = DateTime.UtcNow;
        usuario.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new MensajeUsuarioResponse("¡Correo verificado correctamente!", usuario.ADto()));
    }

    // ---------------------------------------------------------------
    // POST /api/auth/verificar-correo/reenviar
    // ---------------------------------------------------------------
    [Authorize]
    [HttpPost("verificar-correo/reenviar")]
    [EnableRateLimiting("codigos")]
    public async Task<IActionResult> ReenviarVerificacion()
    {
        var usuario = await db.Users.FindAsync(UsuarioId);
        if (usuario is null) return Error(StatusCodes.Status401Unauthorized, "Tu sesión ya no es válida.");

        if (usuario.EmailVerifiedAt is not null)
            return ReglaNegocio("Tu correo ya está verificado.");

        var codigo = await codigos.GenerarAsync(usuario.Email, TiposCodigo.Verificacion);
        if (!await email.EnviarCodigoAsync(usuario.Email, codigo, TiposCodigo.Verificacion))
            return Error(StatusCodes.Status500InternalServerError, "No se pudo enviar el correo. Intenta más tarde.");

        return Ok(new MensajeResponse("Te enviamos un nuevo código de verificación a tu correo."));
    }

    // ---------------------------------------------------------------
    // POST /api/auth/recuperar
    // ---------------------------------------------------------------
    [HttpPost("recuperar")]
    [EnableRateLimiting("codigos")]
    public async Task<IActionResult> Recuperar(RecuperarRequest req)
    {
        var correo = Validaciones.NormalizarEmail(req.Email);
        if (!await db.Users.AnyAsync(u => u.Email == correo))
            return NoEncontrado("No encontramos ninguna cuenta con ese correo.");

        var codigo = await codigos.GenerarAsync(correo, TiposCodigo.Reset);
        if (!await email.EnviarCodigoAsync(correo, codigo, TiposCodigo.Reset))
            return Error(StatusCodes.Status500InternalServerError, "No se pudo enviar el correo. Intenta más tarde.");

        return Ok(new MensajeResponse("Te enviamos un código de 6 dígitos a tu correo. Expira en 15 minutos."));
    }

    // ---------------------------------------------------------------
    // POST /api/auth/recuperar/verificar
    // ---------------------------------------------------------------
    [HttpPost("recuperar/verificar")]
    [EnableRateLimiting("verificar-codigo")]
    [ProducesResponseType<ResetTokenResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> RecuperarVerificar(RecuperarVerificarRequest req)
    {
        var correo = Validaciones.NormalizarEmail(req.Email);

        var resultado = await codigos.VerificarAsync(correo, TiposCodigo.Reset, req.Codigo);
        if (resultado != ResultadoCodigo.Valido)
            return Invalido("codigo", CodigoService.Mensaje(resultado));

        var usuario = await db.Users.FirstOrDefaultAsync(u => u.Email == correo);
        if (usuario is null) return NoEncontrado("No encontramos ninguna cuenta con ese correo.");

        return Ok(new ResetTokenResponse(tokens.CrearTokenReset(usuario)));
    }

    // ---------------------------------------------------------------
    // POST /api/auth/recuperar/restablecer
    // ---------------------------------------------------------------
    [HttpPost("recuperar/restablecer")]
    public async Task<IActionResult> Restablecer(RestablecerRequest req)
    {
        var correo = Validaciones.NormalizarEmail(req.Email);
        var usuario = await db.Users.FirstOrDefaultAsync(u => u.Email == correo);

        if (usuario is null || !await tokens.ValidarTokenResetAsync(req.ResetToken, usuario))
            return Invalido("resetToken", "La sesión de recuperación expiró o no es válida. Vuelve a solicitar el código.");

        usuario.Password = BCrypt.Net.BCrypt.HashPassword(req.Password);
        usuario.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        // Por si quedó algún código de recuperación pendiente
        await db.PasswordResetCodes
            .Where(c => c.Email == correo && c.Tipo == TiposCodigo.Reset)
            .ExecuteDeleteAsync();
        intentos.Limpiar(correo);

        // Contraseña nueva → se cierran TODAS las sesiones abiertas (por si alguien más tenía acceso)
        await sesiones.RevocarTodosAsync(usuario.Id);
        await auditoria.RegistrarAsync(AccionesBitacora.PasswordRestablecida, usuario.Id, correo,
            "Restablecida con código de recuperación; sesiones revocadas");

        return Ok(new MensajeResponse("Contraseña actualizada correctamente. Inicia sesión."));
    }

    /// <summary>Verifica con BCrypt; si el hash está corrupto devuelve false en lugar de lanzar excepción.</summary>
    internal static bool VerificarPassword(string password, string hash)
    {
        try { return BCrypt.Net.BCrypt.Verify(password, hash); }
        catch (BCrypt.Net.SaltParseException) { return false; }
    }
}
