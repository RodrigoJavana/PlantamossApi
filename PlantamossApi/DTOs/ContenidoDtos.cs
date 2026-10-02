using System.ComponentModel.DataAnnotations;
using PlantamossApi.Helpers;

namespace PlantamossApi.DTOs;

// ------------------------------------------------------------------
// DTOs de contenido: estados, plantas, noticias y blogs.
// ------------------------------------------------------------------

public record EstadoDto(int IdEstado, string NombreEstado, string? ClimaPredominante, int CantidadPlantas);

public record PlantaDto(
    int IdPlanta,
    string NombrePlanta,
    string? NombreCientifico,
    string LetraInicial,
    int? IdEstado,
    string? NombreEstado,
    string? UrlImagen,
    string TipoClima,
    string LuzSol,
    string? Riego,
    string? Descripcion,
    string? CuidadosTexto,
    DateTime? CreatedAt,
    DateTime? UpdatedAt);

/// <summary>Respuesta de GET /estados/{id}/plantas.</summary>
public record EstadoConPlantasDto(EstadoDto Estado, List<PlantaDto> Plantas);

public record NoticiaDto(
    int IdNoticia,
    string Titulo,
    string Contenido,
    string? UrlImagen,
    string? UrlExterna,
    bool Destacada,
    bool Activa,
    long? IdEditor,
    string? NombreEditor,
    DateTime FechaPublicacion);

public record BlogDto(
    int IdBlog,
    long? IdUsuario,
    string Titulo,
    string Contenido,
    string? Contenido2,
    string? Contenido3,
    string? UrlImagen,
    string? UrlImagen2,
    string? UrlImagen3,
    string? UrlImagen4,
    string? UrlImagen5,
    DateTime FechaPublicacion,
    string Autor);

/// <summary>Body de POST/PUT /plantas.</summary>
public class PlantaRequest
{
    [Required(ErrorMessage = "El nombre de la planta es obligatorio.")]
    [MaxLength(255, ErrorMessage = "El nombre no puede superar 255 caracteres.")]
    public string NombrePlanta { get; set; } = "";

    [MaxLength(255, ErrorMessage = "El nombre científico no puede superar 255 caracteres.")]
    public string? NombreCientifico { get; set; }

    public int? IdEstado { get; set; }

    [MaxLength(500, ErrorMessage = "La URL de la imagen no puede superar 500 caracteres.")]
    [UrlHttp(PermitirRelativa = true, ErrorMessage = "La URL de la imagen no es válida (debe empezar con http://, https:// o /).")]
    public string? UrlImagen { get; set; }

    /// <summary>tropical | arido | templado | frio (si viene vacío se usa "templado").</summary>
    [ValoresPermitidos("tropical", "arido", "templado", "frio", ErrorMessage = "El tipo de clima debe ser: tropical, arido, templado o frio.")]
    public string? TipoClima { get; set; }

    /// <summary>sol_directo | sombra_parcial | sombra (si viene vacío se usa "sol_directo").</summary>
    [ValoresPermitidos("sol_directo", "sombra_parcial", "sombra", ErrorMessage = "La luz solar debe ser: sol_directo, sombra_parcial o sombra.")]
    public string? LuzSol { get; set; }

    [MaxLength(100, ErrorMessage = "El riego no puede superar 100 caracteres.")]
    public string? Riego { get; set; }

    [MaxLength(5000, ErrorMessage = "La descripción no puede superar 5000 caracteres.")]
    public string? Descripcion { get; set; }

    [MaxLength(5000, ErrorMessage = "Los cuidados no pueden superar 5000 caracteres.")]
    public string? CuidadosTexto { get; set; }
}

/// <summary>Body de POST/PUT /editor/noticias.</summary>
public class NoticiaRequest
{
    [Required(ErrorMessage = "El título es obligatorio.")]
    [MaxLength(255, ErrorMessage = "El título no puede superar 255 caracteres.")]
    public string Titulo { get; set; } = "";

    [Required(ErrorMessage = "El contenido es obligatorio.")]
    [MaxLength(10000, ErrorMessage = "El contenido no puede superar 10000 caracteres.")]
    public string Contenido { get; set; } = "";

    [MaxLength(500, ErrorMessage = "La URL de la imagen no puede superar 500 caracteres.")]
    [UrlHttp(PermitirRelativa = true, ErrorMessage = "La URL de la imagen no es válida (debe empezar con http://, https:// o /).")]
    public string? UrlImagen { get; set; }

    [MaxLength(500, ErrorMessage = "La URL externa no puede superar 500 caracteres.")]
    [UrlHttp(ErrorMessage = "La URL externa no es válida (debe empezar con http:// o https://).")]
    public string? UrlExterna { get; set; }

    public bool Destacada { get; set; }
}

/// <summary>Body de POST/PUT /blogs.</summary>
public class BlogRequest
{
    [Required(ErrorMessage = "El título es obligatorio.")]
    [MaxLength(255, ErrorMessage = "El título no puede superar 255 caracteres.")]
    public string Titulo { get; set; } = "";

    [Required(ErrorMessage = "El contenido es obligatorio.")]
    [MinLength(20, ErrorMessage = "El contenido debe tener al menos 20 caracteres.")]
    [MaxLength(10000, ErrorMessage = "El contenido no puede superar 10000 caracteres.")]
    public string Contenido { get; set; } = "";

    [MaxLength(10000, ErrorMessage = "El contenido no puede superar 10000 caracteres.")]
    public string? Contenido2 { get; set; }
    [MaxLength(10000, ErrorMessage = "El contenido no puede superar 10000 caracteres.")]
    public string? Contenido3 { get; set; }

    [Required(ErrorMessage = "La imagen principal es obligatoria.")]
    [MaxLength(500, ErrorMessage = "La URL de la imagen no puede superar 500 caracteres.")]
    [UrlHttp(PermitirRelativa = true, ErrorMessage = "La URL de la imagen principal no es válida (debe empezar con http://, https:// o /).")]
    public string UrlImagen { get; set; } = "";

    [MaxLength(500, ErrorMessage = "La URL de la imagen no puede superar 500 caracteres.")]
    [UrlHttp(PermitirRelativa = true, ErrorMessage = "La URL de la imagen no es válida (debe empezar con http://, https:// o /).")]
    public string? UrlImagen2 { get; set; }
    [MaxLength(500, ErrorMessage = "La URL de la imagen no puede superar 500 caracteres.")]
    [UrlHttp(PermitirRelativa = true, ErrorMessage = "La URL de la imagen no es válida (debe empezar con http://, https:// o /).")]
    public string? UrlImagen3 { get; set; }
    [MaxLength(500, ErrorMessage = "La URL de la imagen no puede superar 500 caracteres.")]
    [UrlHttp(PermitirRelativa = true, ErrorMessage = "La URL de la imagen no es válida (debe empezar con http://, https:// o /).")]
    public string? UrlImagen4 { get; set; }
    [MaxLength(500, ErrorMessage = "La URL de la imagen no puede superar 500 caracteres.")]
    [UrlHttp(PermitirRelativa = true, ErrorMessage = "La URL de la imagen no es válida (debe empezar con http://, https:// o /).")]
    public string? UrlImagen5 { get; set; }
}
