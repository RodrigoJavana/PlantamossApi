namespace PlantamossApi.Models;

/// <summary>
/// Usuario del sistema (tabla <c>users</c>).
/// El mapeo a columnas snake_case está en <see cref="Data.AppDbContext"/>.
/// </summary>
public class User
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    /// <summary>Siempre se guarda en minúsculas.</summary>
    public string Email { get; set; } = "";
    public DateTime? EmailVerifiedAt { get; set; }
    /// <summary>Hash bcrypt de la contraseña (nunca la contraseña en texto plano).</summary>
    public string Password { get; set; } = "";
    /// <summary>user | editor | noticias | admin</summary>
    public string Role { get; set; } = Roles.User;
    /// <summary>activo | suspendido</summary>
    public string Estado { get; set; } = EstadosCuenta.Activo;
    /// <summary>Claves de las ciudades fijadas en el clima de Inicio (null = las de por defecto).</summary>
    public string[]? CiudadesClima { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>Roles válidos (deben coincidir con el CHECK de la BD).</summary>
public static class Roles
{
    public const string User = "user";
    public const string Editor = "editor";
    public const string Noticias = "noticias";
    public const string Admin = "admin";

    public static readonly string[] Todos = [User, Editor, Noticias, Admin];
}

/// <summary>Estados de cuenta válidos.</summary>
public static class EstadosCuenta
{
    public const string Activo = "activo";
    public const string Suspendido = "suspendido";

    public static readonly string[] Todos = [Activo, Suspendido];
}
