using System.Text.Json.Serialization;

namespace PlantamossApi.DTOs;

/// <summary>
/// Forma ÚNICA de los errores de la API (ver CONTRATO.md):
/// <code>{ "message": "texto", "errors": { "campo": ["msg"] } }</code>
/// <c>errors</c> solo aparece en errores de validación (422).
/// <c>traceId</c> solo aparece en errores 500: es el X-Correlation-Id para buscar el error en los logs.
/// </summary>
public class ApiError(string message, Dictionary<string, string[]>? errors = null, string? traceId = null)
{
    public string Message { get; } = message;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string[]>? Errors { get; } = errors;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TraceId { get; } = traceId;
}

/// <summary>Respuesta simple <c>{ message }</c> para operaciones exitosas.</summary>
public record MensajeResponse(string Message);
