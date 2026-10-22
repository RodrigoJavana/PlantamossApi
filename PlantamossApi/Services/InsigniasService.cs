using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PlantamossApi.Data;
using PlantamossApi.DTOs;

namespace PlantamossApi.Services;

/// <summary>De dónde sale el avance de una insignia.</summary>
public enum MedicionInsignia
{
    /// <summary>Referencias distintas registradas para un evento (<see cref="DefinicionInsignia.Evento"/>).</summary>
    EventosDistintos,
    /// <summary>Blogs publicados por el usuario (tabla <c>miniblogs</c>).</summary>
    BlogsPublicados,
    /// <summary>Racha máxima del usuario (tabla <c>rachas</c>).</summary>
    RachaMaxima,
    /// <summary>Entrar en una fecha especial de México (<see cref="DefinicionInsignia.Fechas"/>, día local de México).</summary>
    FechaEspecial,
}

/// <summary>Definición fija de una insignia del catálogo. Textos y metas según CONTRATO.md, sección 7.</summary>
public record DefinicionInsignia(
    string Codigo,
    string Titulo,
    string Descripcion,
    string Icono,
    string Categoria,
    int Meta,
    MedicionInsignia Medicion,
    string? Evento = null,
    string? Tono = null,
    (int Mes, int Dia)[]? Fechas = null);

/// <summary>Resultado de registrar un evento: válido con las insignias nuevas, o el campo y mensaje del error.</summary>
public record ResultadoEvento(bool Valido, string? Campo = null, string? Mensaje = null, List<InsigniaDto>? Nuevas = null);

/// <summary>
/// Insignias (logros) del usuario (tablas <c>eventos_usuario</c> e <c>insignias_usuario</c>).
/// <para>
/// El catálogo es estático. Los eventos (ver una planta, un estado o leer un blog) se guardan una vez por referencia,
/// así repetirlos no suma. <see cref="RevisarAsync"/> calcula los avances con pocas consultas y desbloquea
/// las insignias que ya cumplen la meta. El armado del DTO (<see cref="ADto"/>) es estático y puro.
/// </para>
/// </summary>
public class InsigniasService(AppDbContext db)
{
    /// <summary>Eventos que cuentan para las insignias. Cualquier otro valor se rechaza con 400.</summary>
    public static readonly string[] EventosPermitidos = ["VER_PLANTA", "VER_ESTADO", "LEER_BLOG"];

    /// <summary>Catálogo fijo de insignias, en el orden en que se muestran.</summary>
    public static readonly IReadOnlyList<DefinicionInsignia> Catalogo =
    [
        new("primer_brote", "Primer brote", "Conoce tu primera planta del catálogo.", "brote", "Plantas", 1, MedicionInsignia.EventosDistintos, "VER_PLANTA"),
        new("amigo_del_nopal", "Amigo del nopal", "Conoce 10 plantas mexicanas.", "hoja", "Plantas", 10, MedicionInsignia.EventosDistintos, "VER_PLANTA"),
        new("coleccionista", "Muy buen conocedor", "Conoce 30 plantas mexicanas.", "flor", "Plantas", 30, MedicionInsignia.EventosDistintos, "VER_PLANTA"),
        new("viajero", "Viajero de la República", "Explora 5 estados en el mapa.", "mapa", "Mapa", 5, MedicionInsignia.EventosDistintos, "VER_ESTADO"),
        new("de_norte_a_sur", "De Baja California a Yucatán", "Explora los 32 estados de México.", "brujula", "Mapa", 32, MedicionInsignia.EventosDistintos, "VER_ESTADO"),
        new("buen_lector", "Buen lector", "Lee 5 blogs de la comunidad.", "libro", "Blog", 5, MedicionInsignia.EventosDistintos, "LEER_BLOG"),
        new("cronista_verde", "Escritor verde", "Publica tu primer blog.", "escribir", "Blog", 1, MedicionInsignia.BlogsPublicados),
        new("pulgar_verde", "Pulgar verde", "Entra a la app 7 días seguidos.", "gota", "Racha", 7, MedicionInsignia.RachaMaxima),
        new("corazon_de_agave", "Corazón de agave", "Entra a la app 30 días seguidos.", "corazon", "Racha", 30, MedicionInsignia.RachaMaxima),
        // Fechas especiales: se ganan al entrar (web o app) ese día
        new("flor_de_cempasuchil", "Flor de cempasúchil", "Entra a Plantamoss en Día de Muertos (1 o 2 de noviembre).", "sol", "Fechas", 1,
            MedicionInsignia.FechaEspecial, Tono: "rojo", Fechas: [(11, 1), (11, 2)]),
        new("nochebuena", "Flor de Nochebuena", "Entra a Plantamoss en Navidad (24 o 25 de diciembre).", "nochebuena", "Fechas", 1,
            MedicionInsignia.FechaEspecial, Tono: "navidad", Fechas: [(12, 24), (12, 25)]),
        new("jacaranda", "Jacaranda de primavera", "Entra a Plantamoss al iniciar la primavera (20 o 21 de marzo).", "jacaranda", "Fechas", 1,
            MedicionInsignia.FechaEspecial, Tono: "morado", Fechas: [(3, 20), (3, 21)]),
    ];

    /// <summary>
    /// Registra un evento del usuario. Valida el tipo de evento y que la planta, el estado o el blog exista.
    /// Repetir el mismo evento con la misma referencia no cuenta doble (lo ignora la UNIQUE de la tabla).
    /// Después revisa las insignias y devuelve las que se desbloquearon en esta llamada.
    /// </summary>
    public async Task<ResultadoEvento> RegistrarEventoAsync(long userId, string evento, string referencia)
    {
        if (!EventosPermitidos.Contains(evento))
            return new ResultadoEvento(false, "evento", "El evento no está permitido.");

        // Solo se acepta un entero positivo y se guarda normalizado ("007" y "7" son la misma planta)
        if (!int.TryParse(referencia, NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id <= 0)
            return new ResultadoEvento(false, "referencia", MensajeInexistente(evento));

        var existe = evento switch
        {
            "VER_PLANTA" => await db.Plantas.AnyAsync(p => p.IdPlanta == id),
            "VER_ESTADO" => await db.Estados.AnyAsync(e => e.IdEstado == id),
            _ => await db.Miniblogs.AnyAsync(m => m.IdBlog == id),
        };
        if (!existe) return new ResultadoEvento(false, "referencia", MensajeInexistente(evento));

        var referenciaNormalizada = id.ToString(CultureInfo.InvariantCulture);

        // INSERT ... ON CONFLICT DO NOTHING: si el usuario ya registró este evento con esta referencia, no pasa nada
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO eventos_usuario (user_id, evento, referencia) VALUES ({userId}, {evento}, {referenciaNormalizada}) ON CONFLICT DO NOTHING");

        var (_, nuevas) = await RevisarAsync(userId);
        return new ResultadoEvento(true, Nuevas: nuevas);
    }

    /// <summary>
    /// Calcula el avance de todas las insignias, desbloquea las que ya cumplen la meta y aún no estaban desbloqueadas.
    /// Devuelve (todas, nuevas): <c>nuevas</c> son solo las desbloqueadas en esta llamada.
    /// <paramref name="hoy"/> (día local de México) se puede fijar en pruebas; por defecto es hoy.
    /// </summary>
    public async Task<(List<InsigniaDto> Todas, List<InsigniaDto> Nuevas)> RevisarAsync(long userId, DateOnly? hoy = null)
    {
        var dia = hoy ?? RachaService.Hoy();

        // 1. Insignias ya desbloqueadas (codigo -> fecha)
        var desbloqueadas = await db.InsigniasUsuario.AsNoTracking()
            .Where(i => i.UserId == userId)
            .ToDictionaryAsync(i => i.Codigo, i => i.DesbloqueadaEn);

        // 2. Referencias distintas por evento (una consulta con COUNT(DISTINCT))
        var eventos = await db.EventosUsuario.AsNoTracking()
            .Where(e => e.UserId == userId)
            .GroupBy(e => e.Evento)
            .Select(g => new { Evento = g.Key, Cantidad = g.Select(x => x.Referencia).Distinct().Count() })
            .ToDictionaryAsync(x => x.Evento, x => x.Cantidad);

        // 3. Blogs publicados por el usuario
        var blogs = await db.Miniblogs.AsNoTracking().CountAsync(m => m.IdUsuario == userId);

        // 4. Racha máxima (0 si el usuario nunca ha entrado)
        var rachaMaxima = await db.Rachas.AsNoTracking()
            .Where(r => r.UserId == userId)
            .Select(r => (int?)r.RachaMaxima)
            .FirstOrDefaultAsync() ?? 0;

        // 5. Desbloquear las que cumplen la meta. ON CONFLICT DO NOTHING evita duplicados si otra petición gana la carrera;
        //    solo cuenta como nueva si el INSERT afectó una fila.
        var ahora = DateTime.UtcNow;
        var nuevas = new List<InsigniaDto>();
        foreach (var def in Catalogo)
        {
            if (desbloqueadas.ContainsKey(def.Codigo)) continue;
            var valor = Valor(def, eventos, blogs, rachaMaxima, dia);
            if (valor < def.Meta) continue;

            var insertadas = await db.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO insignias_usuario (user_id, codigo, desbloqueada_en) VALUES ({userId}, {def.Codigo}, {ahora}) ON CONFLICT DO NOTHING");
            if (insertadas == 0) continue;

            desbloqueadas[def.Codigo] = ahora;
            nuevas.Add(ADto(def, valor, ahora));
        }

        // 6. Armar la lista completa con el avance actual
        var todas = Catalogo
            .Select(def => ADto(def,
                Valor(def, eventos, blogs, rachaMaxima, dia),
                desbloqueadas.TryGetValue(def.Codigo, out var fecha) ? fecha : (DateTime?)null))
            .ToList();

        return (todas, nuevas);
    }

    /// <summary>Valor bruto de la medición de una insignia (sin tope). Si no hay datos vale 0.</summary>
    private static int Valor(DefinicionInsignia def, IReadOnlyDictionary<string, int> eventos, int blogs, int rachaMaxima, DateOnly hoy) =>
        def.Medicion switch
        {
            MedicionInsignia.EventosDistintos => eventos.GetValueOrDefault(def.Evento!),
            MedicionInsignia.BlogsPublicados => blogs,
            MedicionInsignia.FechaEspecial => EsFechaEspecial(def, hoy) ? 1 : 0,
            _ => rachaMaxima,
        };

    /// <summary>True si <paramref name="hoy"/> es una de las fechas (mes, día) de la insignia.</summary>
    public static bool EsFechaEspecial(DefinicionInsignia def, DateOnly hoy) =>
        def.Fechas?.Any(f => f.Mes == hoy.Month && f.Dia == hoy.Day) == true;

    /// <summary>
    /// Arma el DTO. El avance tiene tope en la meta y el progreso va de 0 a 100 (entero, redondeado hacia abajo).
    /// </summary>
    public static InsigniaDto ADto(DefinicionInsignia def, int valor, DateTime? desbloqueadaEn)
    {
        // Una insignia ganada siempre se muestra completa (las de Fechas valen 0 fuera de su día).
        var avance = desbloqueadaEn is not null ? def.Meta : Math.Min(valor, def.Meta);
        return new InsigniaDto
        {
            Codigo = def.Codigo,
            Titulo = def.Titulo,
            Descripcion = def.Descripcion,
            Icono = def.Icono,
            Categoria = def.Categoria,
            Tono = def.Tono ?? def.Categoria,
            Meta = def.Meta,
            Avance = avance,
            Progreso = avance * 100 / def.Meta,
            Desbloqueada = desbloqueadaEn is not null,
            DesbloqueadaEn = desbloqueadaEn is { } f
                ? DateTime.SpecifyKind(f, DateTimeKind.Utc).ToString("O", CultureInfo.InvariantCulture)
                : null,
        };
    }

    private static string MensajeInexistente(string evento) => evento switch
    {
        "VER_PLANTA" => "La planta no existe.",
        "VER_ESTADO" => "El estado no existe.",
        _ => "El blog no existe.",
    };
}
