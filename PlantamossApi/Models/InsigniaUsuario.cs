namespace PlantamossApi.Models;

/// <summary>
/// Insignia que el usuario ya desbloqueó (tabla <c>insignias_usuario</c>).
/// La PK es (user_id, codigo): cada insignia se desbloquea una sola vez. El código debe existir en el catálogo de <c>InsigniasService</c>.
/// </summary>
public class InsigniaUsuario
{
    public long UserId { get; set; }
    public string Codigo { get; set; } = "";
    public DateTime DesbloqueadaEn { get; set; }

    public User? Usuario { get; set; }
}
