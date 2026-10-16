using Microsoft.Extensions.Caching.Memory;

namespace PlantamossApi.Services;

/// <summary>
/// Bloqueo TEMPORAL de cuenta por correo tras varios intentos de login fallidos (en memoria).
/// <list type="bullet">
/// <item>Los fallos se cuentan dentro de una ventana de <c>Seguridad:MinutosBloqueo</c> (15 min).</item>
/// <item>Al llegar a <c>Seguridad:MaxIntentosLogin</c> (5) el correo queda bloqueado <c>MinutosBloqueo</c> minutos:
///       el login responde 429 aunque la contraseña sea correcta.</item>
/// <item>Un login correcto (o restablecer la contraseña) limpia el contador.</item>
/// </list>
/// Además, Program.cs aplica un rate limiter por IP a /auth/login (20/min).
/// </summary>
public class LoginIntentosService(IMemoryCache cache, SeguridadOptions opciones)
{
    public int MaxFallos => Math.Max(1, opciones.MaxIntentosLogin);
    private TimeSpan Duracion => TimeSpan.FromMinutes(Math.Max(1, opciones.MinutosBloqueo));

    private sealed class Contador
    {
        public int Fallos;
        public DateTime VentanaExpira;
        public DateTime? BloqueadoHasta;
    }

    private static string Clave(string email) => "login-fallos:" + email;

    /// <summary>Devuelve los minutos que faltan de bloqueo (null = no está bloqueado).</summary>
    public int? MinutosRestantes(string email)
    {
        if (!cache.TryGetValue(Clave(email), out Contador? c) || c?.BloqueadoHasta is null) return null;
        var resta = c.BloqueadoHasta.Value - DateTime.UtcNow;
        return resta > TimeSpan.Zero ? (int)Math.Ceiling(resta.TotalMinutes) : null;
    }

    public bool EstaBloqueado(string email) => MinutosRestantes(email) is not null;

    /// <summary>Suma un fallo. Devuelve true si con este fallo la cuenta QUEDÓ bloqueada.</summary>
    public bool RegistrarFallo(string email)
    {
        var clave = Clave(email);
        var ahora = DateTime.UtcNow;
        lock (this)
        {
            if (!cache.TryGetValue(clave, out Contador? c) || c is null || (c.VentanaExpira <= ahora && c.BloqueadoHasta is null))
                c = new Contador { VentanaExpira = ahora.Add(Duracion) };

            c.Fallos++;
            var recienBloqueado = false;
            if (c.Fallos >= MaxFallos && c.BloqueadoHasta is null)
            {
                c.BloqueadoHasta = ahora.Add(Duracion);
                recienBloqueado = true;
            }

            var expira = c.BloqueadoHasta ?? c.VentanaExpira;
            cache.Set(clave, c, expira);
            return recienBloqueado;
        }
    }

    public void Limpiar(string email) => cache.Remove(Clave(email));
}
