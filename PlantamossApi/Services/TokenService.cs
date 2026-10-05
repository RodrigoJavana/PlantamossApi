using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using PlantamossApi.Models;

namespace PlantamossApi.Services;

/// <summary>Configuración de la sección "Jwt" de appsettings.</summary>
public class JwtOptions
{
    /// <summary>Clave secreta para firmar (HMAC-SHA256). Mínimo 32 caracteres.</summary>
    public string Key { get; set; } = "";
    public string Issuer { get; set; } = "plantamoss-api";
    public string Audience { get; set; } = "plantamoss-front";
    /// <summary>Vida del token de acceso en minutos (CONTRATO 5.1: 60 por defecto).</summary>
    public int ExpiracionMinutos { get; set; } = 60;

    /// <summary>Audiencia distinta para los tokens de recuperación, así no sirven como sesión.</summary>
    public string AudienceReset => Audience + "-reset";

    public SymmetricSecurityKey ClaveFirma() => new(Encoding.UTF8.GetBytes(Key));
}

/// <summary>
/// Genera y valida los JWT de la API:
/// <list type="bullet">
/// <item>Token de acceso (Jwt:ExpiracionMinutos, 60 min por defecto) con claims sub, email, name, role, jti y stamp.</item>
/// <item>Refresh token opaco (64 bytes aleatorios, base64url): ver <see cref="SesionService"/>.</item>
/// <item>Token de recuperación (15 min) con claim purpose=reset, para /auth/recuperar/restablecer.</item>
/// </list>
/// </summary>
public class TokenService(JwtOptions opciones)
{
    public const string ClaimProposito = "purpose";
    public const string ClaimSello = "stamp";

    /// <summary>Segundos de vida del token de acceso (campo <c>expiresIn</c> de la respuesta).</summary>
    public int SegundosVidaAcceso => opciones.ExpiracionMinutos * 60;

    /// <summary>
    /// Crea el token de acceso del usuario. El claim "stamp" se deriva del hash de la contraseña:
    /// si la contraseña cambia, todos los tokens de acceso anteriores dejan de valer (Program.cs → OnTokenValidated).
    /// </summary>
    public string CrearTokenSesion(User usuario)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = opciones.Issuer,
            Audience = opciones.Audience,
            Expires = DateTime.UtcNow.AddMinutes(opciones.ExpiracionMinutos),
            SigningCredentials = new SigningCredentials(opciones.ClaveFirma(), SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = usuario.Id.ToString(),
                [JwtRegisteredClaimNames.Email] = usuario.Email,
                [JwtRegisteredClaimNames.Name] = usuario.Name,
                ["role"] = usuario.Role,
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString("N"),
                [ClaimSello] = Sello(usuario.Password),
            },
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    /// <summary>
    /// Crea el token corto de recuperación de contraseña.
    /// Incluye un "sello" derivado del hash actual de la contraseña: en cuanto la contraseña cambia,
    /// el token deja de servir (así solo se puede usar una vez).
    /// </summary>
    public string CrearTokenReset(User usuario)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = opciones.Issuer,
            Audience = opciones.AudienceReset,
            Expires = DateTime.UtcNow.AddMinutes(15),
            SigningCredentials = new SigningCredentials(opciones.ClaveFirma(), SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Email] = usuario.Email,
                [ClaimProposito] = "reset",
                [ClaimSello] = Sello(usuario.Password),
            },
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    /// <summary>
    /// Valida el token de recuperación: firma, expiración, propósito, correo y que la contraseña no haya cambiado.
    /// </summary>
    public async Task<bool> ValidarTokenResetAsync(string token, User usuario)
    {
        var resultado = await new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = opciones.Issuer,
            ValidAudience = opciones.AudienceReset,
            IssuerSigningKey = opciones.ClaveFirma(),
            ClockSkew = TimeSpan.FromSeconds(30),
        });

        if (!resultado.IsValid) return false;

        var claims = resultado.Claims;
        return claims.TryGetValue(ClaimProposito, out var proposito) && proposito?.ToString() == "reset"
            && claims.TryGetValue(JwtRegisteredClaimNames.Email, out var email) && email?.ToString() == usuario.Email
            && claims.TryGetValue(ClaimSello, out var sello) && sello?.ToString() == Sello(usuario.Password);
    }

    /// <summary>Huella corta del hash de la contraseña (no revela la contraseña ni el hash bcrypt completo).</summary>
    public static string Sello(string hashPassword) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(hashPassword)))[..16];

    // -----------------------------------------------------------------
    // Refresh tokens (CONTRATO 5.1)
    // -----------------------------------------------------------------

    /// <summary>Genera un refresh token: 64 bytes de un generador criptográfico, en base64url.</summary>
    public static string GenerarRefreshToken() =>
        Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));

    /// <summary>SHA-256 en hex minúsculas (64 caracteres): es lo ÚNICO que se guarda en la BD.</summary>
    public static string HashRefreshToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
}
