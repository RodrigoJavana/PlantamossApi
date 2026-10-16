namespace PlantamossApi.Models;

/// <summary>
/// Refresh token de una sesión (tabla <c>refresh_tokens</c>, CONTRATO 5.1).
/// En la BD solo se guarda el HASH SHA-256 (hex, 64 caracteres) del token; el token en claro
/// únicamente lo conoce el cliente. Si alguien roba la BD no puede usar los tokens.
/// </summary>
public class RefreshToken
{
    public long Id { get; set; }
    public long UserId { get; set; }
    /// <summary>SHA-256 en hexadecimal (minúsculas) del token entregado al cliente.</summary>
    public string TokenHash { get; set; } = "";
    public DateTime ExpiraEn { get; set; }
    public DateTime CreadoEn { get; set; }
    /// <summary>Fecha en que se revocó (rotación, logout, cambio de contraseña...). Null = vigente.</summary>
    public DateTime? RevocadoEn { get; set; }
    /// <summary>Hash del token que lo reemplazó al rotar (permite seguir la cadena).</summary>
    public string? ReemplazadoPor { get; set; }
    public string? Ip { get; set; }
    public string? UserAgent { get; set; }

    public User? Usuario { get; set; }
}
