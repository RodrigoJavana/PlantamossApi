namespace PlantamossApi.DTOs;

/// <summary>Estado de la racha del usuario (respuesta de GET y POST /api/racha).</summary>
public class RachaDto
{
    /// <summary>Días seguidos vigentes (0 si la racha ya se perdió).</summary>
    public int RachaActual { get; set; }
    public int RachaMaxima { get; set; }
    public int TotalDias { get; set; }
    /// <summary>Fecha "yyyy-MM-dd" (día local de México) o null si nunca ha registrado una visita.</summary>
    public string? UltimoDia { get; set; }
    /// <summary>True si ya se registró la visita de hoy.</summary>
    public bool RegistradaHoy { get; set; }
    /// <summary>Etapa de la planta (0 a 5).</summary>
    public int Etapa { get; set; }
    public string NombreEtapa { get; set; } = "";
    /// <summary>Días que faltan para la siguiente etapa; null en la última.</summary>
    public int? DiasSiguienteEtapa { get; set; }
}
