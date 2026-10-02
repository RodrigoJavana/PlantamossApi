using System.Linq.Expressions;
using PlantamossApi.DTOs;
using PlantamossApi.Models;

namespace PlantamossApi.Helpers;

/// <summary>
/// Conversión de entidades a DTOs.
/// Las <c>Expression</c> se usan dentro de <c>.Select(...)</c> para que EF genere el SQL
/// con solo las columnas necesarias (incluyendo los JOIN a estados/usuarios).
/// </summary>
public static class Mapeos
{
    public static UserDto ADto(this User u) =>
        new(u.Id, u.Name, u.Email, u.Role, u.Estado, u.EmailVerifiedAt != null, u.CreatedAt);

    public static readonly Expression<Func<User, UserDto>> UserProyeccion = u =>
        new UserDto(u.Id, u.Name, u.Email, u.Role, u.Estado, u.EmailVerifiedAt != null, u.CreatedAt);

    public static readonly Expression<Func<Estado, EstadoDto>> EstadoProyeccion = e =>
        new EstadoDto(e.IdEstado, e.NombreEstado, e.ClimaPredominante, e.Plantas.Count());

    public static readonly Expression<Func<Planta, PlantaDto>> PlantaProyeccion = p =>
        new PlantaDto(
            p.IdPlanta, p.NombrePlanta, p.NombreCientifico, p.LetraInicial,
            p.IdEstado, p.Estado != null ? p.Estado.NombreEstado : null,
            p.UrlImagen, p.TipoClima, p.LuzSol, p.Riego, p.Descripcion, p.CuidadosTexto,
            p.CreatedAt, p.UpdatedAt);

    public static readonly Expression<Func<Noticia, NoticiaDto>> NoticiaProyeccion = n =>
        new NoticiaDto(
            n.IdNoticia, n.Titulo, n.Contenido, n.UrlImagen, n.UrlExterna,
            n.Destacada, n.Activa, n.IdEditor, n.Editor != null ? n.Editor.Name : null,
            n.FechaPublicacion);

    public static readonly Expression<Func<Miniblog, BlogDto>> BlogProyeccion = b =>
        new BlogDto(
            b.IdBlog, b.IdUsuario, b.Titulo, b.Contenido, b.Contenido2, b.Contenido3,
            b.UrlImagen, b.UrlImagen2, b.UrlImagen3, b.UrlImagen4, b.UrlImagen5,
            b.FechaPublicacion, b.Usuario != null ? b.Usuario.Name : "Anónimo");
}
