namespace PlantamossApi.DTOs;

/// <summary>Clima actual (OpenWeather). Si <c>Ok</c> es false, el resto viene en null.</summary>
public record ClimaDto(
    bool Ok,
    double? Temp,
    double? Sensacion,
    int? Humedad,
    double? Viento,
    string? Descripcion,
    string? Icono)
{
    public static ClimaDto Fallo() => new(false, null, null, null, null, null, null);
}

/// <summary>Video de YouTube.</summary>
public record VideoDto(string VideoId, string Titulo, string Canal, string? Miniatura, DateTime? FechaPublicacion);
