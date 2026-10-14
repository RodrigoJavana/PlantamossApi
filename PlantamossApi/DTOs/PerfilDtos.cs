using System.ComponentModel.DataAnnotations;
using PlantamossApi.Helpers;

namespace PlantamossApi.DTOs;

public class ActualizarPerfilRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(255, ErrorMessage = "El nombre no puede superar 255 caracteres.")]
    public string Name { get; set; } = "";
}

public class CambiarPasswordRequest
{
    [Required(ErrorMessage = "Ingresa tu contraseña actual.")]
    [MaxLength(128, ErrorMessage = "La contraseña no puede superar 128 caracteres.")]
    [SinRecortar]
    public string PasswordActual { get; set; } = "";

    [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
    [PasswordFuerte]
    [MaxLength(72, ErrorMessage = "La contraseña no puede superar 72 caracteres.")]
    [SinRecortar]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Confirma tu nueva contraseña.")]
    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
    [SinRecortar]
    public string PasswordConfirmation { get; set; } = "";
}

public class CiudadesClimaRequest
{
    [Required(ErrorMessage = "Selecciona al menos una ciudad.")]
    [MaxLength(20, ErrorMessage = "Demasiadas ciudades.")]
    public List<string> Ciudades { get; set; } = [];
}

public class EliminarCuentaRequest
{
    [Required(ErrorMessage = "Ingresa tu contraseña para confirmar.")]
    [MaxLength(128, ErrorMessage = "La contraseña no puede superar 128 caracteres.")]
    [SinRecortar]
    public string Password { get; set; } = "";
}
