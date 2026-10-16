namespace PlantamossApi.Models;

/// <summary>
/// Registro de la bitácora de auditoría (tabla <c>bitacora_auditoria</c>, CONTRATO 5.2).
/// Guarda QUIÉN hizo QUÉ, CUÁNDO y DESDE DÓNDE. Nunca guarda contraseñas ni tokens.
/// </summary>
public class BitacoraAuditoria
{
    public long Id { get; set; }
    public DateTime Fecha { get; set; }
    public long? UserId { get; set; }
    public string? Email { get; set; }
    public string Accion { get; set; } = "";
    public string? Detalle { get; set; }
    public string? Ip { get; set; }
    public bool Exito { get; set; } = true;
}

/// <summary>Acciones registradas en la bitácora (CONTRATO 5.2).</summary>
public static class AccionesBitacora
{
    public const string LoginOk = "login_ok";
    public const string LoginFallido = "login_fallido";
    public const string LoginBloqueado = "login_bloqueado";
    public const string Logout = "logout";
    public const string RefreshReuso = "refresh_reuso";
    public const string Registro = "registro";
    public const string PasswordCambiada = "password_cambiada";
    public const string PasswordRestablecida = "password_restablecida";
    public const string UsuarioCreado = "usuario_creado";
    public const string UsuarioEditado = "usuario_editado";
    public const string UsuarioEliminado = "usuario_eliminado";
    public const string PlantaCreada = "planta_creada";
    public const string PlantaEditada = "planta_editada";
    public const string PlantaEliminada = "planta_eliminada";
    public const string NoticiaCreada = "noticia_creada";
    public const string NoticiaEditada = "noticia_editada";
    public const string NoticiaActivada = "noticia_activada";
    public const string NoticiaDesactivada = "noticia_desactivada";
    public const string NoticiaEliminada = "noticia_eliminada";
    public const string BlogEliminado = "blog_eliminado";
}
