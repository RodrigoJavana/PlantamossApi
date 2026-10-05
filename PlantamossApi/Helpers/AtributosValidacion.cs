using System.ComponentModel.DataAnnotations;

namespace PlantamossApi.Helpers;

// ---------------------------------------------------------------------------
// Atributos de validación propios. Se usan en los DTOs igual que [Required]:
// así TODOS los errores de formato salen juntos en un solo 422.
// (Las reglas que necesitan la BD —correo repetido, estado inexistente— se
//  revisan en el controlador.)
// ---------------------------------------------------------------------------

/// <summary>El texto (si viene) debe ser una URL absoluta http:// o https://.</summary>
[AttributeUsage(AttributeTargets.Property)]
public class UrlHttpAttribute : ValidationAttribute
{
    public UrlHttpAttribute() : base("La URL no es válida (debe empezar con http:// o https://).") { }

    /// <summary>Si es true, también acepta rutas del sitio como "/img/foto.jpg" (para imágenes).</summary>
    public bool PermitirRelativa { get; set; }

    public override bool IsValid(object? value) =>
        value is not string texto || string.IsNullOrWhiteSpace(texto)
        || (PermitirRelativa ? Validaciones.EsRutaImagenValida(texto.Trim()) : Validaciones.EsUrlValida(texto.Trim()));
}

/// <summary>El texto (si viene) debe ser uno de los valores indicados.</summary>
[AttributeUsage(AttributeTargets.Property)]
public class ValoresPermitidosAttribute(params string[] valores) : ValidationAttribute
{
    public string[] Valores { get; } = valores;

    public override bool IsValid(object? value) =>
        value is not string texto || string.IsNullOrWhiteSpace(texto) || Valores.Contains(texto);

    public override string FormatErrorMessage(string name) =>
        ErrorMessage ?? $"El valor debe ser uno de: {string.Join(", ", Valores)}.";
}

/// <summary>
/// Contraseña fuerte: ≥8 caracteres, mayúscula, minúscula, número y carácter especial.
/// El mensaje indica exactamente qué requisitos faltan.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class PasswordFuerteAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        var errores = Validaciones.ErroresPassword(value as string);
        if (errores.Count == 0) return ValidationResult.Success;

        var faltan = string.Join(" ", errores);
        return new ValidationResult(faltan, context.MemberName is null ? null : [context.MemberName]);
    }
}

/// <summary>El booleano debe ser true (checkbox de "acepto").</summary>
[AttributeUsage(AttributeTargets.Property)]
public class DebeSerVerdaderoAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) => value is true;
}

/// <summary>Si el texto tiene algo escrito, debe medir al menos N caracteres (vacío = válido).</summary>
[AttributeUsage(AttributeTargets.Property)]
public class MinimoSiTieneValorAttribute(int minimo) : ValidationAttribute
{
    public int Minimo { get; } = minimo;

    public override bool IsValid(object? value) =>
        value is not string texto || texto.Length == 0 || texto.Length >= Minimo;
}

/// <summary>
/// Marca un texto que NO debe recortarse (trim) al sanear la entrada: contraseñas.
/// Ver <see cref="SanitizarEntradasFilter"/>. Los caracteres de control se siguen rechazando.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class SinRecortarAttribute : Attribute { }
