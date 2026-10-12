using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using PlantamossApi.Models;

namespace PlantamossApi.Services;

/// <summary>Configuración de la sección "Smtp" de appsettings.</summary>
public class SmtpOptions
{
    /// <summary>Servidor SMTP (ej. smtp.gmail.com). Si está vacío, los correos se escriben en el log.</summary>
    public string Host { get; set; } = "";
    public int Port { get; set; } = 465;
    public string User { get; set; } = "";
    public string Password { get; set; } = "";
    /// <summary>Remitente. Si está vacío se usa <see cref="User"/>.</summary>
    public string From { get; set; } = "";
    public string FromName { get; set; } = "Plantamoss";
    public bool EnableSsl { get; set; } = true;
}

/// <summary>
/// Envía correos con MailKit. En desarrollo (sin Smtp:Host) solo escribe el código en la consola.
/// </summary>
public class EmailService(SmtpOptions opciones, ILogger<EmailService> logger)
{
    /// <summary>
    /// Envía el código de 6 dígitos al correo. Devuelve false si el envío falló (el error queda en el log).
    /// </summary>
    public async Task<bool> EnviarCodigoAsync(string email, string codigo, string tipo)
    {
        var esReset = tipo == TiposCodigo.Reset;
        var asunto = esReset
            ? "Código de recuperación - Plantamoss"
            : "Verifica tu correo - Plantamoss";

        // Modo desarrollo: no hay SMTP configurado → mostrar el código en la consola
        if (string.IsNullOrWhiteSpace(opciones.Host))
        {
            logger.LogWarning(
                "\n==================== CORREO (SMTP no configurado) ====================\n" +
                "  Para: {Email}\n  Asunto: {Asunto}\n  CÓDIGO ({Tipo}): {Codigo}\n" +
                "======================================================================",
                email, asunto, tipo, codigo);
            return true;
        }

        try
        {
            var mensaje = new MimeMessage();
            var remitente = string.IsNullOrWhiteSpace(opciones.From) ? opciones.User : opciones.From;
            mensaje.From.Add(new MailboxAddress(opciones.FromName, remitente));
            mensaje.To.Add(MailboxAddress.Parse(email));
            mensaje.Subject = asunto;
            mensaje.Body = new BodyBuilder
            {
                HtmlBody = PlantillaCodigo(codigo, esReset),
                TextBody = $"Tu código de Plantamoss es: {codigo}\nExpira en {CodigoService.MinutosExpiracion} minutos.",
            }.ToMessageBody();

            using var cliente = new SmtpClient();
            // Puerto 465 = SSL directo; 587 = STARTTLS; sin SSL = sin cifrado (solo pruebas locales)
            var seguridad = !opciones.EnableSsl ? SecureSocketOptions.None
                : opciones.Port == 465 ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls;

            await cliente.ConnectAsync(opciones.Host, opciones.Port, seguridad);
            if (!string.IsNullOrWhiteSpace(opciones.User))
                await cliente.AuthenticateAsync(opciones.User, opciones.Password);
            await cliente.SendAsync(mensaje);
            await cliente.DisconnectAsync(true);

            logger.LogInformation("Correo ({Tipo}) enviado a {Email}", tipo, email);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo enviar el correo ({Tipo}) a {Email}", tipo, email);
            return false;
        }
    }

    /// <summary>
    /// Plantilla HTML del correo (réplica de resources/views/emails/codigo-recuperacion.blade.php del proyecto Laravel).
    /// </summary>
    private static string PlantillaCodigo(string codigo, bool esReset)
    {
        var titulo = esReset ? "Recuperación de contraseña" : "Verificación de correo";
        var texto = esReset
            ? "Has solicitado restablecer tu contraseña en Planta-Moss. Usa el siguiente código de 6 dígitos para continuar con el proceso:"
            : "Gracias por registrarte en Planta-Moss. Usa el siguiente código de 6 dígitos para verificar tu correo electrónico:";
        var pie = esReset
            ? "Si no solicitaste este cambio, ignora este correo."
            : "Si no creaste una cuenta, ignora este correo.";

        return $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <title>{{titulo}}</title>
            </head>
            <body style="font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px;">
                <div style="max-width: 600px; margin: 0 auto; background-color: #ffffff; padding: 40px; border-radius: 8px; box-shadow: 0 4px 6px rgba(0,0,0,0.1); text-align: center;">
                    <h1 style="color: #2e7d32; margin-bottom: 20px;">{{titulo}}</h1>
                    <p style="color: #555; font-size: 16px; line-height: 1.5; margin-bottom: 30px;">
                        {{texto}}
                    </p>

                    <div style="background-color: #e8f5e9; border: 2px dashed #4caf50; padding: 20px; font-size: 32px; font-weight: bold; color: #2e7d32; letter-spacing: 5px; margin-bottom: 30px; border-radius: 8px;">
                        {{codigo}}
                    </div>

                    <p style="color: #777; font-size: 14px;">
                        Este código expirará en {{CodigoService.MinutosExpiracion}} minutos.<br>
                        {{pie}}
                    </p>
                </div>
            </body>
            </html>
            """;
    }
}
