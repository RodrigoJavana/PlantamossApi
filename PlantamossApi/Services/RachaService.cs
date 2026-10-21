using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PlantamossApi.Data;
using PlantamossApi.DTOs;
using PlantamossApi.Models;

namespace PlantamossApi.Services;

/// <summary>
/// Racha de días seguidos (tabla <c>rachas</c>) con la planta que crece.
/// <para>
/// El "día" es la fecha local de México. La lógica de fechas, etapas y armado del DTO es estática y pura
/// (<see cref="FechaLocal"/>, <see cref="Etapa"/>, <see cref="DiasSiguienteEtapa"/>, <see cref="ADto"/>),
/// así se puede probar sin base de datos.
/// </para>
/// </summary>
public class RachaService(AppDbContext db)
{
    /// <summary>Días seguidos en los que la planta cambia de etapa (índice = etapa).</summary>
    public static readonly int[] Umbrales = [0, 1, 3, 7, 14, 30];

    /// <summary>Nombre de cada etapa, en el mismo orden que <see cref="Umbrales"/>.</summary>
    public static readonly string[] NombresEtapas = ["Semilla", "Brote", "Plántula", "Planta joven", "Planta adulta", "En flor"];

    private static readonly TimeZoneInfo ZonaMexico = ObtenerZonaMexico();

    /// <summary>Hoy, en la fecha local de México.</summary>
    public static DateOnly Hoy() => FechaLocal(DateTime.UtcNow);

    /// <summary>Convierte un instante UTC a fecha local de México.</summary>
    public static DateOnly FechaLocal(DateTime utc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), ZonaMexico));

    /// <summary>Etapa (0 a 5) de la planta según los días de racha.</summary>
    public static int Etapa(int rachaActual)
    {
        for (var i = Umbrales.Length - 1; i > 0; i--)
        {
            if (rachaActual >= Umbrales[i]) return i;
        }
        return 0;
    }

    /// <summary>Días que faltan para la siguiente etapa; null si ya está en la última.</summary>
    public static int? DiasSiguienteEtapa(int rachaActual)
    {
        var etapa = Etapa(rachaActual);
        return etapa == Umbrales.Length - 1 ? (int?)null : Umbrales[etapa + 1] - rachaActual;
    }

    /// <summary>
    /// Arma la respuesta. Si la última visita fue antes de ayer, la racha se muestra en 0 (se perdió),
    /// pero no se escribe nada en la BD.
    /// </summary>
    public static RachaDto ADto(Racha racha, DateOnly hoy)
    {
        var ayer = hoy.AddDays(-1);
        var vigente = racha.UltimoDia is { } ultimo && ultimo >= ayer;
        var actual = vigente ? racha.RachaActual : 0;
        var etapa = Etapa(actual);

        return new RachaDto
        {
            RachaActual = actual,
            RachaMaxima = racha.RachaMaxima,
            TotalDias = racha.TotalDias,
            UltimoDia = racha.UltimoDia?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            RegistradaHoy = racha.UltimoDia == hoy,
            Etapa = etapa,
            NombreEtapa = NombresEtapas[etapa],
            DiasSiguienteEtapa = DiasSiguienteEtapa(actual),
        };
    }

    /// <summary>Estado actual de la racha. No modifica la BD.</summary>
    public async Task<RachaDto> ObtenerAsync(long userId)
    {
        var racha = await db.Rachas.AsNoTracking().FirstOrDefaultAsync(r => r.UserId == userId)
                    ?? new Racha { UserId = userId };
        return ADto(racha, Hoy());
    }

    /// <summary>
    /// Registra la visita de hoy. Es idempotente: solo la primera visita del día cambia la racha.
    /// </summary>
    public async Task<RachaDto> RegistrarAsync(long userId)
    {
        var hoy = Hoy();
        var ayer = hoy.AddDays(-1);

        await AsegurarFilaAsync(userId);

        // Todo en una transacción y con UPDATE condicionado: si dos peticiones del mismo usuario llegan
        // a la vez, la segunda espera el bloqueo de la fila, re-evalúa el WHERE y ya no cuenta el día otra vez.
        await using var tx = await db.Database.BeginTransactionAsync();

        var cambiados = await db.Rachas
            .Where(r => r.UserId == userId && (r.UltimoDia == null || r.UltimoDia < hoy))
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.RachaActual, r => r.UltimoDia == ayer ? r.RachaActual + 1 : 1)
                .SetProperty(r => r.TotalDias, r => r.TotalDias + 1)
                .SetProperty(r => r.UltimoDia, (DateOnly?)hoy)
                .SetProperty(r => r.ActualizadoEn, DateTime.UtcNow));

        if (cambiados > 0)
        {
            // racha_maxima = max(racha_maxima, racha_actual), ya con el racha_actual nuevo
            await db.Rachas
                .Where(r => r.UserId == userId && r.RachaMaxima < r.RachaActual)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.RachaMaxima, r => r.RachaActual));
        }

        await tx.CommitAsync();

        var racha = await db.Rachas.AsNoTracking().FirstAsync(r => r.UserId == userId);
        return ADto(racha, hoy);
    }

    /// <summary>
    /// Crea la fila del usuario si no existe. Si otra petición la inserta entre el AnyAsync y el INSERT,
    /// la inserción choca por PK (23505): se descarta el intento y se usa la fila que ya existe.
    /// </summary>
    private async Task AsegurarFilaAsync(long userId)
    {
        if (await db.Rachas.AsNoTracking().AnyAsync(r => r.UserId == userId)) return;

        db.Rachas.Add(new Racha { UserId = userId, ActualizadoEn = DateTime.UtcNow });
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            db.ChangeTracker.Clear();
        }
    }

    /// <summary>
    /// Zona horaria de México: "America/Mexico_City" (Linux/ICU) o "Central Standard Time (Mexico)" (Windows).
    /// Si ninguna existe se usa UTC-6 fijo (México no usa horario de verano desde 2022).
    /// </summary>
    private static TimeZoneInfo ObtenerZonaMexico()
    {
        foreach (var id in new[] { "America/Mexico_City", "Central Standard Time (Mexico)" })
        {
            if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zona)) return zona;
        }
        return TimeZoneInfo.CreateCustomTimeZone("Mexico UTC-6", TimeSpan.FromHours(-6), "Mexico UTC-6", "Mexico UTC-6");
    }
}
