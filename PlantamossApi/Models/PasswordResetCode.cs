namespace PlantamossApi.Models;

/// <summary>
/// Código de 6 dígitos enviado por correo (tabla <c>password_reset_codes</c>).
/// Sirve para recuperar contraseña (tipo "reset") y para verificar el correo (tipo "verificacion").
/// </summary>
public class PasswordResetCode
{
    public long Id { get; set; }
    public string Email { get; set; } = "";
    public string Code { get; set; } = "";
    public string Tipo { get; set; } = TiposCodigo.Reset;
    public int Intentos { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Tipos de código permitidos.</summary>
public static class TiposCodigo
{
    public const string Reset = "reset";
    public const string Verificacion = "verificacion";
}
