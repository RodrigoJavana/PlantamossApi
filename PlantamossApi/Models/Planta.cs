namespace PlantamossApi.Models;

/// <summary>Planta del catálogo de flora (tabla <c>catalogo_flora</c>).</summary>
public class Planta
{
    public int IdPlanta { get; set; }
    public string NombrePlanta { get; set; } = "";
    public string? NombreCientifico { get; set; }
    /// <summary>Primera letra de NombrePlanta, en mayúscula y sin acento.</summary>
    public string LetraInicial { get; set; } = "";
    public int? IdEstado { get; set; }
    public Estado? Estado { get; set; }
    public string? UrlImagen { get; set; }
    public string TipoClima { get; set; } = TiposClima.Templado;
    public string LuzSol { get; set; } = TiposLuz.SolDirecto;
    public string? Riego { get; set; }
    public string? Descripcion { get; set; }
    public string? CuidadosTexto { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Valores permitidos para tipo_clima.</summary>
public static class TiposClima
{
    public const string Tropical = "tropical";
    public const string Arido = "arido";
    public const string Templado = "templado";
    public const string Frio = "frio";

    public static readonly string[] Todos = [Tropical, Arido, Templado, Frio];
}

/// <summary>Valores permitidos para luz_sol.</summary>
public static class TiposLuz
{
    public const string SolDirecto = "sol_directo";
    public const string SombraParcial = "sombra_parcial";
    public const string Sombra = "sombra";

    public static readonly string[] Todos = [SolDirecto, SombraParcial, Sombra];
}
