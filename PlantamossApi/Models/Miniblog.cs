namespace PlantamossApi.Models;

/// <summary>Entrada del mini blog (tabla <c>miniblogs</c>).</summary>
public class Miniblog
{
    public int IdBlog { get; set; }
    public long? IdUsuario { get; set; }
    public User? Usuario { get; set; }
    public string Titulo { get; set; } = "";
    public string Contenido { get; set; } = "";
    public string? Contenido2 { get; set; }
    public string? Contenido3 { get; set; }
    public string? UrlImagen { get; set; }
    public string? UrlImagen2 { get; set; }
    public string? UrlImagen3 { get; set; }
    public string? UrlImagen4 { get; set; }
    public string? UrlImagen5 { get; set; }
    public DateTime FechaPublicacion { get; set; }
}
