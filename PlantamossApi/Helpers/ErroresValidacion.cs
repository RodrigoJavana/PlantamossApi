using PlantamossApi.DTOs;

namespace PlantamossApi.Helpers;

/// <summary>
/// Acumulador de errores de validación de negocio.
/// Uso en un controlador:
/// <code>
/// var errores = new ErroresValidacion();
/// if (...) errores.Agregar("email", "Este correo ya está registrado.");
/// if (errores.HayErrores) return UnprocessableEntity(errores.ComoApiError());
/// </code>
/// </summary>
public class ErroresValidacion
{
    private readonly Dictionary<string, List<string>> _errores = [];

    public bool HayErrores => _errores.Count > 0;

    /// <summary>Agrega un mensaje al campo indicado (el nombre se convierte a camelCase).</summary>
    public void Agregar(string campo, string mensaje)
    {
        campo = Validaciones.ACamelCase(campo);
        if (!_errores.TryGetValue(campo, out var lista))
        {
            lista = [];
            _errores[campo] = lista;
        }
        if (!lista.Contains(mensaje)) lista.Add(mensaje);
    }

    public void Agregar(string campo, IEnumerable<string> mensajes)
    {
        foreach (var m in mensajes) Agregar(campo, m);
    }

    /// <summary>Construye el cuerpo <c>{ message, errors }</c> del 422.</summary>
    public ApiError ComoApiError(string mensaje = "Datos inválidos.") =>
        new(mensaje, _errores.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray()));
}
