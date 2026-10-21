using System.ComponentModel.DataAnnotations;

namespace PlantamossApi.DTOs;

// ------------------------------------------------------------------
// DTOs de insignias (logros). Contrato: CONTRATO.md, sección 7.
// ------------------------------------------------------------------

/// <summary>Insignia con el avance del usuario autenticado.</summary>
public class InsigniaDto
{
    public string Codigo { get; set; } = "";
    public string Titulo { get; set; } = "";
    public string Descripcion { get; set; } = "";
    /// <summary>Clave del icono; cada cliente la dibuja con su set (lucide en web, Material Icons en app).</summary>
    public string Icono { get; set; } = "";
    public string Categoria { get; set; } = "";
    /// <summary>Color de la medalla: la categoría o, en las de Fechas, rojo / navidad / morado.</summary>
    public string Tono { get; set; } = "";
    public int Meta { get; set; }
    /// <summary>Avance hasta la meta (tope = meta).</summary>
    public int Avance { get; set; }
    /// <summary>0 a 100.</summary>
    public int Progreso { get; set; }
    public bool Desbloqueada { get; set; }
    /// <summary>Fecha ISO-8601 (UTC) en que se desbloqueó, o null.</summary>
    public string? DesbloqueadaEn { get; set; }
}

/// <summary>Respuesta de GET /api/insignias.</summary>
public class InsigniasRespuestaDto
{
    /// <summary>Todas las insignias del catálogo con el avance del usuario.</summary>
    public List<InsigniaDto> Insignias { get; set; } = [];
    /// <summary>Solo las desbloqueadas en esta llamada (para el aviso "Ganaste una insignia").</summary>
    public List<InsigniaDto> Nuevas { get; set; } = [];
}

/// <summary>Body de POST /api/insignias/evento.</summary>
public class EventoInsigniaRequest
{
    /// <summary>VER_PLANTA | VER_ESTADO | LEER_BLOG (cualquier otro valor responde 400).</summary>
    [Required(ErrorMessage = "El evento es obligatorio.")]
    [MaxLength(30, ErrorMessage = "El evento no puede superar 30 caracteres.")]
    public string Evento { get; set; } = "";

    /// <summary>Id de la planta, del estado o del blog.</summary>
    [Required(ErrorMessage = "La referencia es obligatoria.")]
    [MaxLength(64, ErrorMessage = "La referencia no puede superar 64 caracteres.")]
    public string Referencia { get; set; } = "";
}

/// <summary>Respuesta de POST /api/insignias/evento.</summary>
public class EventoInsigniaRespuestaDto
{
    /// <summary>Insignias desbloqueadas en esta llamada.</summary>
    public List<InsigniaDto> Nuevas { get; set; } = [];
}
