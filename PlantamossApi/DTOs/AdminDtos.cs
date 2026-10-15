using System.ComponentModel.DataAnnotations;
using PlantamossApi.Helpers;

namespace PlantamossApi.DTOs;

public class CrearUsuarioRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(255, ErrorMessage = "El nombre no puede superar 255 caracteres.")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo válido.")]
    [MaxLength(255, ErrorMessage = "El correo no puede superar 255 caracteres.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "El rol es obligatorio.")]
    [ValoresPermitidos("user", "editor", "noticias", "admin", ErrorMessage = "El rol debe ser: user, editor, noticias o admin.")]
    public string Role { get; set; } = "";

    [Required(ErrorMessage = "El estado es obligatorio.")]
    [ValoresPermitidos("activo", "suspendido", ErrorMessage = "El estado debe ser: activo o suspendido.")]
    public string Estado { get; set; } = "";

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    [MaxLength(72, ErrorMessage = "La contraseña no puede superar 72 caracteres.")]
    [SinRecortar]
    public string Password { get; set; } = "";
}

public class EditarUsuarioRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(255, ErrorMessage = "El nombre no puede superar 255 caracteres.")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "El rol es obligatorio.")]
    [ValoresPermitidos("user", "editor", "noticias", "admin", ErrorMessage = "El rol debe ser: user, editor, noticias o admin.")]
    public string Role { get; set; } = "";

    [Required(ErrorMessage = "El estado es obligatorio.")]
    [ValoresPermitidos("activo", "suspendido", ErrorMessage = "El estado debe ser: activo o suspendido.")]
    public string Estado { get; set; } = "";

    /// <summary>Opcional: vacío o null = no cambiar la contraseña.</summary>
    [MinimoSiTieneValor(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    [MaxLength(72, ErrorMessage = "La contraseña no puede superar 72 caracteres.")]
    [SinRecortar]
    public string? Password { get; set; }
}

public record UsuariosPorRolDto(int User, int Editor, int Noticias, int Admin);

public record EstadisticasDto(
    int TotalUsuarios,
    int UsuariosActivos,
    int UsuariosSuspendidos,
    int TotalPlantas,
    int TotalNoticias,
    int TotalBlogs,
    UsuariosPorRolDto UsuariosPorRol);

/// <summary>Registro de la bitácora tal como lo ve el panel (CONTRATO 5.2).</summary>
public record BitacoraDto(long Id, DateTime Fecha, long? UserId, string? Email, string Accion, string? Detalle, string? Ip, bool Exito);

/// <summary>Página de resultados: <c>{ total, pagina, tamano, items }</c>.</summary>
public record PaginaDto<T>(int Total, int Pagina, int Tamano, List<T> Items);
