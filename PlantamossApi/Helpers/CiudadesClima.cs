namespace PlantamossApi.Helpers;

/// <summary>Ciudad disponible para el widget de clima (una capital por estado).</summary>
/// <param name="Id">Clave estable que se guarda en users.ciudades_clima.</param>
/// <param name="Consulta">Texto que se manda a OpenWeather (q=...).</param>
public record CiudadClima(string Id, string Ciudad, string Estado, string Consulta);

/// <summary>
/// Catálogo de ciudades que el usuario puede fijar en el clima de Inicio.
/// </summary>
public static class CiudadesClima
{
    public const int Maximo = 6;

    public static readonly CiudadClima[] Todas =
    [
        new("cdmx", "Ciudad de México", "Ciudad de México", "Mexico City,MX"),
        new("guadalajara", "Guadalajara", "Jalisco", "Guadalajara,MX"),
        new("monterrey", "Monterrey", "Nuevo León", "Monterrey,MX"),
        new("oaxaca", "Oaxaca", "Oaxaca", "Oaxaca,MX"),
        new("merida", "Mérida", "Yucatán", "Merida,MX"),
        new("chihuahua", "Chihuahua", "Chihuahua", "Chihuahua,MX"),
        new("aguascalientes", "Aguascalientes", "Aguascalientes", "Aguascalientes,MX"),
        new("mexicali", "Mexicali", "Baja California", "Mexicali,MX"),
        new("la-paz", "La Paz", "Baja California Sur", "La Paz,MX"),
        new("campeche", "Campeche", "Campeche", "Campeche,MX"),
        new("tuxtla", "Tuxtla Gutiérrez", "Chiapas", "Tuxtla Gutierrez,MX"),
        new("saltillo", "Saltillo", "Coahuila", "Saltillo,MX"),
        new("colima", "Colima", "Colima", "Colima,MX"),
        new("durango", "Durango", "Durango", "Durango,MX"),
        new("guanajuato", "Guanajuato", "Guanajuato", "Guanajuato,MX"),
        new("chilpancingo", "Chilpancingo", "Guerrero", "Chilpancingo,MX"),
        new("pachuca", "Pachuca", "Hidalgo", "Pachuca,MX"),
        new("toluca", "Toluca", "Estado de México", "Toluca,MX"),
        new("morelia", "Morelia", "Michoacán", "Morelia,MX"),
        new("cuernavaca", "Cuernavaca", "Morelos", "Cuernavaca,MX"),
        new("tepic", "Tepic", "Nayarit", "Tepic,MX"),
        new("puebla", "Puebla", "Puebla", "Puebla,MX"),
        new("queretaro", "Querétaro", "Querétaro", "Queretaro,MX"),
        new("chetumal", "Chetumal", "Quintana Roo", "Chetumal,MX"),
        new("san-luis-potosi", "San Luis Potosí", "San Luis Potosí", "San Luis Potosi,MX"),
        new("culiacan", "Culiacán", "Sinaloa", "Culiacan,MX"),
        new("hermosillo", "Hermosillo", "Sonora", "Hermosillo,MX"),
        new("villahermosa", "Villahermosa", "Tabasco", "Villahermosa,MX"),
        new("ciudad-victoria", "Ciudad Victoria", "Tamaulipas", "Ciudad Victoria,MX"),
        new("tlaxcala", "Tlaxcala", "Tlaxcala", "Tlaxcala,MX"),
        new("xalapa", "Xalapa", "Veracruz", "Xalapa,MX"),
        new("zacatecas", "Zacatecas", "Zacatecas", "Zacatecas,MX"),
    ];

    /// <summary>Las 6 ciudades que se muestran si el usuario no ha configurado nada (las del sitio original).</summary>
    public static readonly string[] PorDefecto = ["cdmx", "guadalajara", "monterrey", "oaxaca", "merida", "chihuahua"];

    private static readonly Dictionary<string, CiudadClima> PorId = Todas.ToDictionary(c => c.Id);

    public static bool Existe(string id) => PorId.ContainsKey(id);

    /// <summary>Convierte las claves guardadas en ciudades, en el mismo orden (ignora claves que ya no existan).</summary>
    public static List<CiudadClima> Resolver(IEnumerable<string>? ids) =>
        (ids is null || !ids.Any() ? PorDefecto : ids)
            .Where(PorId.ContainsKey)
            .Select(id => PorId[id])
            .ToList();
}
