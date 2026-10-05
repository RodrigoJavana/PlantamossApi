using System.Globalization;
using System.Text;

namespace PlantamossApi.Helpers;

/// <summary>
/// Reglas de validación de negocio reutilizables (las que no cubren los atributos [Required], etc.).
/// </summary>
public static class Validaciones
{
    /// <summary>
    /// Revisa que la contraseña sea fuerte: mínimo 8 caracteres, mayúscula, minúscula, número y carácter especial.
    /// Devuelve la lista de requisitos que NO se cumplen (vacía = contraseña válida).
    /// </summary>
    public static List<string> ErroresPassword(string? password)
    {
        var errores = new List<string>();
        password ??= "";

        if (password.Length < 8) errores.Add("La contraseña debe tener al menos 8 caracteres.");
        if (!password.Any(char.IsUpper)) errores.Add("La contraseña debe incluir al menos una letra mayúscula.");
        if (!password.Any(char.IsLower)) errores.Add("La contraseña debe incluir al menos una letra minúscula.");
        if (!password.Any(char.IsDigit)) errores.Add("La contraseña debe incluir al menos un número.");
        if (password.All(char.IsLetterOrDigit)) errores.Add("La contraseña debe incluir al menos un carácter especial.");

        return errores;
    }

    /// <summary>
    /// True si el texto es una ruta de imagen válida: URL http/https o ruta relativa del sitio
    /// que empieza con "/" (ej. "/img/agave-azul.jpeg", como las imágenes del seed).
    /// </summary>
    public static bool EsRutaImagenValida(string? url) =>
        EsUrlValida(url)
        || (url is not null && url.StartsWith('/') && !url.StartsWith("//") && !url.Contains('\\') && !url.Any(char.IsWhiteSpace));

    /// <summary>True si el texto es una URL absoluta http/https.</summary>
    public static bool EsUrlValida(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    /// <summary>Normaliza un correo: sin espacios y en minúsculas.</summary>
    public static string NormalizarEmail(string? email) => (email ?? "").Trim().ToLowerInvariant();

    /// <summary>Convierte cadenas vacías o de puros espacios en null (y recorta las demás).</summary>
    public static string? VacioANull(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    /// <summary>
    /// Calcula la letra inicial: primera letra del nombre, en mayúscula y sin acento ("Árbol" → "A").
    /// </summary>
    public static string LetraInicial(string nombre)
    {
        var primera = nombre.Trim().FirstOrDefault(char.IsLetterOrDigit);
        if (primera == default) return "#";

        // Descomponer (Á → A + ´) y quedarnos solo con la letra base
        var sinAcento = primera.ToString()
            .Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .FirstOrDefault(primera);

        return char.ToUpperInvariant(sinAcento).ToString();
    }

    /// <summary>Convierte "NombrePlanta" o "$.nombrePlanta" en "nombrePlanta" (claves de errores en camelCase).</summary>
    public static string ACamelCase(string clave)
    {
        if (clave.StartsWith("$.")) clave = clave[2..];
        else if (clave == "$") clave = "";
        if (string.IsNullOrEmpty(clave)) return clave;

        // Para claves anidadas ("Usuario.Nombre") convertimos cada segmento
        return string.Join('.', clave.Split('.').Select(s =>
            s.Length == 0 ? s : char.ToLowerInvariant(s[0]) + s[1..]));
    }
}
