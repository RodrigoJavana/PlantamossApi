using System.ComponentModel.DataAnnotations;
using PlantamossApi.Helpers;

namespace PlantamossApi.DTOs;

// ------------------------------------------------------------------
// DTOs de autenticación (/api/auth). Los nombres de propiedad se
// serializan en camelCase automáticamente (name, passwordConfirmation...).
// ------------------------------------------------------------------

/// <summary>Usuario tal como lo ve el front.</summary>
public record UserDto(
    long Id,
    string Name,
    string Email,
    string Role,
    string Estado,
    bool EmailVerificado,
    DateTime CreatedAt);

/// <summary>
/// Respuesta de login, registro y refresh (CONTRATO 5.1): <c>{ token, refreshToken, expiresIn, user }</c>.
/// <c>expiresIn</c> = segundos de vida del token de acceso.
/// </summary>
public record AuthResponse(string Token, string RefreshToken, int ExpiresIn, UserDto User);

/// <summary>Respuesta de verificar correo: <c>{ message, user }</c>.</summary>
public record MensajeUsuarioResponse(string Message, UserDto User);

/// <summary>Respuesta de /auth/recuperar/verificar.</summary>
public record ResetTokenResponse(string ResetToken);

public class RegisterRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(255, ErrorMessage = "El nombre no puede superar 255 caracteres.")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo válido.")]
    [MaxLength(255, ErrorMessage = "El correo no puede superar 255 caracteres.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [PasswordFuerte]
    [MaxLength(72, ErrorMessage = "La contraseña no puede superar 72 caracteres.")]
    [SinRecortar]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Confirma tu contraseña.")]
    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
    [SinRecortar]
    public string PasswordConfirmation { get; set; } = "";

    /// <summary>Debe ser true (aceptó términos y condiciones).</summary>
    [DebeSerVerdadero(ErrorMessage = "Debes aceptar los términos y condiciones.")]
    public bool Terminos { get; set; }

    /// <summary>Debe ser true (aceptó el aviso de privacidad).</summary>
    [DebeSerVerdadero(ErrorMessage = "Debes aceptar el aviso de privacidad.")]
    public bool Privacidad { get; set; }
}

public class LoginRequest
{
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo válido.")]
    [MaxLength(255, ErrorMessage = "El correo no puede superar 255 caracteres.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [MaxLength(128, ErrorMessage = "La contraseña no puede superar 128 caracteres.")]
    [SinRecortar]
    public string Password { get; set; } = "";
}

/// <summary>Body de POST /auth/refresh.</summary>
public class RefreshRequest
{
    [Required(ErrorMessage = "Falta el refresh token.")]
    [MaxLength(200, ErrorMessage = "El refresh token no es válido.")]
    public string RefreshToken { get; set; } = "";
}

/// <summary>Body de POST /auth/logout (el refresh token a revocar; opcional).</summary>
public class LogoutRequest
{
    [MaxLength(200, ErrorMessage = "El refresh token no es válido.")]
    public string? RefreshToken { get; set; }
}

public class VerificarCorreoRequest
{
    [Required(ErrorMessage = "Ingresa el código.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "El código debe tener 6 dígitos.")]
    public string Codigo { get; set; } = "";
}

public class RecuperarRequest
{
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo válido.")]
    [MaxLength(255, ErrorMessage = "El correo no puede superar 255 caracteres.")]
    public string Email { get; set; } = "";
}

public class RecuperarVerificarRequest
{
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo válido.")]
    [MaxLength(255, ErrorMessage = "El correo no puede superar 255 caracteres.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Ingresa el código.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "El código debe tener 6 dígitos.")]
    public string Codigo { get; set; } = "";
}

public class RestablecerRequest
{
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo válido.")]
    [MaxLength(255, ErrorMessage = "El correo no puede superar 255 caracteres.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Falta el token de recuperación.")]
    [MaxLength(2000, ErrorMessage = "El token de recuperación no es válido.")]
    public string ResetToken { get; set; } = "";

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [PasswordFuerte]
    [MaxLength(72, ErrorMessage = "La contraseña no puede superar 72 caracteres.")]
    [SinRecortar]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Confirma tu contraseña.")]
    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
    [SinRecortar]
    public string PasswordConfirmation { get; set; } = "";
}
