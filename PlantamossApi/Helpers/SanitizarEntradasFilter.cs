using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PlantamossApi.Helpers;

/// <summary>
/// Filtro GLOBAL de saneamiento de entradas (validación de entradas, punto 3):
/// <list type="bullet">
/// <item>Recorta espacios (trim) de todos los textos de los DTOs y de los parámetros de query,
///       excepto los marcados con <see cref="SinRecortarAttribute"/> (contraseñas).</item>
/// <item>Rechaza (422) textos con caracteres de control (\0, ESC, etc.; se permiten \t \r \n)
///       y caracteres de control de dirección bidi (U+202A–U+202E, U+2066–U+2069) que sirven
///       para disfrazar texto.</item>
/// </list>
/// Corre ANTES del filtro que convierte el ModelState inválido en 422 (Order -2000), así sus errores
/// salen en la misma respuesta <c>{ message, errors }</c>.
/// </summary>
public class SanitizarEntradasFilter : IActionFilter, IOrderedFilter
{
    public const string MensajeCaracteres = "El texto contiene caracteres no permitidos.";

    public int Order => -3000;

    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> CacheProps = new();

    public void OnActionExecuting(ActionExecutingContext context)
    {
        foreach (var (nombre, valor) in context.ActionArguments.ToList())
        {
            switch (valor)
            {
                case string texto:
                    if (TieneCaracteresProhibidos(texto)) context.ModelState.AddModelError(nombre, MensajeCaracteres);
                    else context.ActionArguments[nombre] = texto.Trim();
                    break;
                case not null when valor.GetType().Namespace == "PlantamossApi.DTOs":
                    Sanear(valor, context);
                    break;
            }
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }

    private static void Sanear(object dto, ActionExecutingContext context)
    {
        var props = CacheProps.GetOrAdd(dto.GetType(), t => t
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && (p.PropertyType == typeof(string) || p.PropertyType == typeof(List<string>)))
            .ToArray());

        foreach (var prop in props)
        {
            var campo = JsonNamingPolicy.CamelCase.ConvertName(prop.Name);
            var recortar = prop.GetCustomAttribute<SinRecortarAttribute>() is null;

            if (prop.GetValue(dto) is string texto)
            {
                if (TieneCaracteresProhibidos(texto)) context.ModelState.AddModelError(campo, MensajeCaracteres);
                else if (recortar) prop.SetValue(dto, texto.Trim());
            }
            else if (prop.GetValue(dto) is List<string> lista)
            {
                for (var i = 0; i < lista.Count; i++)
                {
                    if (lista[i] is null) continue;
                    if (TieneCaracteresProhibidos(lista[i])) context.ModelState.AddModelError(campo, MensajeCaracteres);
                    else lista[i] = lista[i].Trim();
                }
            }
        }
    }

    /// <summary>True si hay caracteres de control (salvo tab y saltos de línea) o de control bidi.</summary>
    public static bool TieneCaracteresProhibidos(string texto)
    {
        foreach (var c in texto)
        {
            if (c is '\t' or '\n' or '\r') continue;
            if (char.IsControl(c)) return true;
            if (c is >= '‪' and <= '‮' or >= '⁦' and <= '⁩') return true;
        }
        return false;
    }
}
