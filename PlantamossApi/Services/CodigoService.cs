using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using PlantamossApi.Data;
using PlantamossApi.Models;

namespace PlantamossApi.Services;

/// <summary>Resultado de verificar un código.</summary>
public enum ResultadoCodigo
{
    Valido,
    Incorrecto,
    Expirado,
    DemasiadosIntentos,
}

/// <summary>
/// Clave secreta ("pepper") para el HMAC de los códigos. Se registra como singleton en Program.cs
/// a partir de Seguridad:PepperCodigos (PEPPER_CODIGOS en el .env).
/// </summary>
public sealed record ClaveCodigos(byte[] Clave);

/// <summary>
/// Maneja los códigos de 6 dígitos de la tabla password_reset_codes
/// (recuperación de contraseña y verificación de correo).
/// <para>
/// ALMACENAMIENTO SEGURO: en la columna <c>code</c> NO se guarda el código, sino
/// HMAC-SHA256(pepper, "tipo:email:código") en hexadecimal. Un código de 6 dígitos solo tiene
/// 1 000 000 combinaciones: sin el pepper secreto (que NO está en la BD) no se puede
/// averiguar por fuerza bruta a partir de un respaldo o una fuga de la base de datos.
/// </para>
/// </summary>
public class CodigoService(AppDbContext db, ClaveCodigos clave)
{
    public const int MinutosExpiracion = 15;
    public const int MaxIntentos = 5;

    /// <summary>
    /// Genera un código nuevo para el correo y tipo indicados.
    /// Borra los códigos anteriores del mismo correo+tipo para que solo el último sea válido.
    /// </summary>
    public async Task<string> GenerarAsync(string email, string tipo)
    {
        // RandomNumberGenerator es criptográficamente seguro (a diferencia de new Random())
        var codigo = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

        await db.PasswordResetCodes
            .Where(c => c.Email == email && c.Tipo == tipo)
            .ExecuteDeleteAsync();

        var ahora = DateTime.UtcNow;
        db.PasswordResetCodes.Add(new PasswordResetCode
        {
            Email = email,
            Code = Hash(email, tipo, codigo),
            Tipo = tipo,
            Intentos = 0,
            ExpiresAt = ahora.AddMinutes(MinutosExpiracion),
            CreatedAt = ahora,
        });
        await db.SaveChangesAsync();

        return codigo;
    }

    /// <summary>
    /// Verifica el código. Si es correcto se elimina (un solo uso).
    /// Si es incorrecto suma un intento; al llegar a <see cref="MaxIntentos"/> el código se invalida.
    /// </summary>
    public async Task<ResultadoCodigo> VerificarAsync(string email, string tipo, string codigo)
    {
        var registro = await db.PasswordResetCodes
            .Where(c => c.Email == email && c.Tipo == tipo)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync();

        if (registro is null) return ResultadoCodigo.Incorrecto;

        if (registro.ExpiresAt < DateTime.UtcNow)
        {
            db.PasswordResetCodes.Remove(registro);
            await db.SaveChangesAsync();
            return ResultadoCodigo.Expirado;
        }

        if (registro.Intentos >= MaxIntentos)
        {
            db.PasswordResetCodes.Remove(registro);
            await db.SaveChangesAsync();
            return ResultadoCodigo.DemasiadosIntentos;
        }

        // Se compara el HMAC del código recibido con el guardado, en TIEMPO CONSTANTE
        // (FixedTimeEquals) para no filtrar información por tiempos de respuesta.
        var correcto = CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.ASCII.GetBytes(registro.Code.Trim()),
            System.Text.Encoding.ASCII.GetBytes(Hash(email, tipo, codigo)));

        if (!correcto)
        {
            registro.Intentos++;
            if (registro.Intentos >= MaxIntentos)
            {
                db.PasswordResetCodes.Remove(registro);
                await db.SaveChangesAsync();
                return ResultadoCodigo.DemasiadosIntentos;
            }
            await db.SaveChangesAsync();
            return ResultadoCodigo.Incorrecto;
        }

        db.PasswordResetCodes.Remove(registro);
        await db.SaveChangesAsync();
        return ResultadoCodigo.Valido;
    }

    /// <summary>HMAC-SHA256(pepper, "tipo:email:código") en hex minúsculas (64 caracteres).</summary>
    private string Hash(string email, string tipo, string codigo) =>
        Convert.ToHexString(HMACSHA256.HashData(clave.Clave,
            System.Text.Encoding.UTF8.GetBytes($"{tipo}:{email}:{codigo}"))).ToLowerInvariant();

    /// <summary>Mensaje en español para un resultado fallido.</summary>
    public static string Mensaje(ResultadoCodigo resultado) => resultado switch
    {
        ResultadoCodigo.Expirado => "El código ha expirado. Solicita uno nuevo.",
        ResultadoCodigo.DemasiadosIntentos => "Demasiados intentos fallidos. Solicita un nuevo código.",
        _ => "El código es incorrecto o ha expirado.",
    };
}
