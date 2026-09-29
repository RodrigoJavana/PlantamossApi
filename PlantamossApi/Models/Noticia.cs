namespace PlantamossApi.Models;

/// <summary>Noticia publicada por un editor (tabla <c>noticias</c>).</summary>
public class Noticia
{
    public int IdNoticia { get; set; }
    public string Titulo { get; set; } = "";
    public string Contenido { get; set; } = "";
    public string? UrlImagen { get; set; }
    public string? UrlExterna { get; set; }
    public bool Destacada { get; set; }
    public bool Activa { get; set; } = true;
    public long? IdEditor { get; set; }
    public User? Editor { get; set; }
    public DateTime FechaPublicacion { get; set; }
}
