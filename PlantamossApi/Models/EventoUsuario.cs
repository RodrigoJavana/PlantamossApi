namespace PlantamossApi.Models;

/// <summary>
/// Evento que cuenta para las insignias (tabla <c>eventos_usuario</c>): planta vista, estado visto o blog leído.
/// La referencia es el id de la planta, del estado o del blog, en texto. Una misma referencia no se repite por usuario y evento.
/// El mapeo a columnas snake_case está en <see cref="Data.AppDbContext"/>.
/// </summary>
public class EventoUsuario
{
    public long Id { get; set; }
    public long UserId { get; set; }
    /// <summary>VER_PLANTA, VER_ESTADO o LEER_BLOG.</summary>
    public string Evento { get; set; } = "";
    /// <summary>Id de la planta, del estado o del blog (máx. 64 caracteres).</summary>
    public string Referencia { get; set; } = "";
    public DateTime CreadoEn { get; set; }

    public User? Usuario { get; set; }
}
