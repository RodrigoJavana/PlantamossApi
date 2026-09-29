namespace PlantamossApi.Models;

/// <summary>Estado de la República Mexicana (tabla <c>estados</c>).</summary>
public class Estado
{
    public int IdEstado { get; set; }
    public string NombreEstado { get; set; } = "";
    public string? ClimaPredominante { get; set; }

    /// <summary>Plantas registradas en este estado (propiedad de navegación).</summary>
    public List<Planta> Plantas { get; set; } = [];
}
