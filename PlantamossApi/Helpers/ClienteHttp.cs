namespace PlantamossApi.Helpers;

/// <summary>Datos del cliente de la petición (IP y User-Agent) normalizados para guardar en la BD.</summary>
public static class ClienteHttp
{
    /// <summary>
    /// IP del cliente. Si la API está detrás de un proxy, ForwardedHeaders (Program.cs) ya puso aquí
    /// la IP real tomada de X-Forwarded-For. Las IPv4 mapeadas a IPv6 (::ffff:1.2.3.4) se devuelven como IPv4.
    /// </summary>
    public static string? Ip(HttpContext? http)
    {
        var ip = http?.Connection.RemoteIpAddress;
        if (ip is null) return null;
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
        var texto = ip.ToString();
        return texto.Length > 45 ? texto[..45] : texto;
    }

    /// <summary>User-Agent recortado a 300 caracteres y sin caracteres de control.</summary>
    public static string? UserAgent(HttpContext? http)
    {
        var ua = http?.Request.Headers.UserAgent.ToString();
        if (string.IsNullOrWhiteSpace(ua)) return null;
        ua = new string(ua.Where(c => !char.IsControl(c)).ToArray()).Trim();
        return ua.Length > 300 ? ua[..300] : ua;
    }
}
