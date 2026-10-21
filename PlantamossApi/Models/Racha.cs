namespace PlantamossApi.Models;

/// <summary>
/// Racha de días seguidos en que el usuario entra a la app (tabla <c>rachas</c>).
/// Una fila por usuario (la PK es el propio <see cref="UserId"/>). El mapeo a columnas snake_case está en <see cref="Data.AppDbContext"/>.
/// </summary>
public class Racha
{
    public long UserId { get; set; }
    /// <summary>Días seguidos hasta <see cref="UltimoDia"/>. Si la última visita fue antes de ayer, la racha ya se perdió.</summary>
    public int RachaActual { get; set; }
    /// <summary>Racha más larga que ha tenido el usuario.</summary>
    public int RachaMaxima { get; set; }
    /// <summary>Total de días distintos en que el usuario ha entrado.</summary>
    public int TotalDias { get; set; }
    /// <summary>Último día (fecha local de México) en que se registró una visita. Null = nunca.</summary>
    public DateOnly? UltimoDia { get; set; }
    public DateTime ActualizadoEn { get; set; }

    public User? Usuario { get; set; }
}
