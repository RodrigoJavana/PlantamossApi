// =====================================================================
//  Plantamoss API (.NET 9)
//  Catálogo de flora mexicana — reemplaza al backend Laravel original.
//  Contrato de rutas/DTOs: Plantamoss2026/CONTRATO.md (secciones 2, 4 y 5).
//  Medidas de seguridad (Unidad II): ver SEGURIDAD.md en la raíz de ApiPlantamoss.
// =====================================================================
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PlantamossApi.Data;
using PlantamossApi.DTOs;
using PlantamossApi.Helpers;
using PlantamossApi.Middleware;
using PlantamossApi.Models;
using PlantamossApi.Services;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

// Logger mínimo para poder registrar errores de arranque (antes de leer la configuración)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    var config = builder.Configuration;
    var esDesarrollo = builder.Environment.IsDevelopment();

    // -----------------------------------------------------------------
    // 1. Configuración: appsettings.json + appsettings.{Entorno}.json + ApiPlantamoss/.env + variables de entorno.
    //    Los SECRETOS (Jwt:Key, PepperCodigos, cadena de conexión real, API keys, SMTP) vienen SOLO del .env
    //    o de variables de entorno; los appsettings versionados los traen vacíos.
    // -----------------------------------------------------------------
    DotEnv.Cargar(config, builder.Environment.ContentRootPath);

    var jwtOpciones = config.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
    if (jwtOpciones.Key.Length < 32)
        throw new InvalidOperationException(
            "Falta la clave JWT (o tiene menos de 32 caracteres). Defínela como JWT_KEY en ApiPlantamoss/.env " +
            "(copia .env.example o ejecuta configurar-env.ps1) o en la variable de entorno Jwt__Key. " +
            "Por seguridad ya NO se lee de appsettings.");
    if (jwtOpciones.ExpiracionMinutos <= 0) jwtOpciones.ExpiracionMinutos = 60;
    builder.Services.AddSingleton(jwtOpciones);

    var seguridad = config.GetSection("Seguridad").Get<SeguridadOptions>() ?? new SeguridadOptions();
    builder.Services.AddSingleton(seguridad);

    // Pepper de los códigos de 6 dígitos (HMAC). Si falta, se deriva de la clave JWT (también secreta).
    byte[] pepper;
    if (string.IsNullOrWhiteSpace(seguridad.PepperCodigos))
    {
        Log.Warning("Seguridad:PepperCodigos (PEPPER_CODIGOS) no está configurado: se deriva de Jwt:Key. Defínelo en el .env.");
        pepper = SHA256.HashData(Encoding.UTF8.GetBytes("plantamoss-codigos|" + jwtOpciones.Key));
    }
    else pepper = Encoding.UTF8.GetBytes(seguridad.PepperCodigos);
    builder.Services.AddSingleton(new ClaveCodigos(pepper));

    builder.Services.AddSingleton(config.GetSection("Smtp").Get<SmtpOptions>() ?? new SmtpOptions());

    // -----------------------------------------------------------------
    // 2. LOGS con Serilog: consola + archivo de texto diario + archivo JSON compacto diario.
    //    Carpeta: PlantamossApi/logs (en .gitignore). Retención: 14 días.
    //    NUNCA se registran cuerpos de petición, contraseñas, tokens ni el header Authorization.
    // -----------------------------------------------------------------
    // En Azure App Service (Linux) el sistema de archivos del proyecto es efímero: en Production se escribe en
    // $HOME/LogFiles/plantamoss (/home/LogFiles es persistente). En otros entornos, carpeta "logs" junto al proyecto.
    var homeAzure = Environment.GetEnvironmentVariable("HOME");
    var carpetaLogs = builder.Environment.IsProduction() && !string.IsNullOrWhiteSpace(homeAzure)
        ? Path.Combine(homeAzure, "LogFiles", "plantamoss")
        : Path.Combine(builder.Environment.ContentRootPath, "logs");
    builder.Host.UseSerilog((ctx, servicios, lc) => lc
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
        .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Aplicacion", "PlantamossApi")
        .Enrich.WithProperty("Entorno", ctx.HostingEnvironment.EnvironmentName)
        .WriteTo.Console(outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {CorrelationId}{NewLine}{Exception}")
        .WriteTo.File(
            path: Path.Combine(carpetaLogs, "plantamoss-.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14,
            shared: true,
            outputTemplate:
                "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] ({CorrelationId}) {SourceContext}: {Message:lj}{NewLine}{Exception}")
        .WriteTo.File(
            new CompactJsonFormatter(),
            path: Path.Combine(carpetaLogs, "plantamoss-.json"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14,
            shared: true));

    // -----------------------------------------------------------------
    // 3. Kestrel: sin header "Server" y límite global del cuerpo (1 MB por defecto → 413)
    // -----------------------------------------------------------------
    builder.WebHost.ConfigureKestrel(k =>
    {
        k.AddServerHeader = false;
        k.Limits.MaxRequestBodySize = seguridad.MaxBodyBytes;
        k.Limits.MaxRequestHeadersTotalSize = 32 * 1024;
    });

    // -----------------------------------------------------------------
    // 4. Proxy inverso (Azure App Service termina el TLS y manda X-Forwarded-Proto / X-Forwarded-For).
    //    Solo se confía en esos headers si Seguridad:DetrasDeProxy=true (si no, solo desde localhost).
    // -----------------------------------------------------------------
    builder.Services.Configure<ForwardedHeadersOptions>(o =>
    {
        o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        o.ForwardLimit = 1;
        if (seguridad.DetrasDeProxy)
        {
            o.KnownNetworks.Clear();
            o.KnownProxies.Clear();
        }
    });

    // HSTS (solo fuera de Development): el navegador recordará usar HTTPS durante 1 año
    builder.Services.AddHsts(o =>
    {
        o.MaxAge = TimeSpan.FromDays(365);
        o.IncludeSubDomains = true;
    });

    // -----------------------------------------------------------------
    // 5. Base de datos (PostgreSQL). El esquema lo crea database/init/*.sql (+ database/migraciones).
    //    EF Core usa SIEMPRE consultas parametrizadas (no hay SQL concatenado en el proyecto).
    // -----------------------------------------------------------------
    var cadenaConexion = config.GetConnectionString("Default");
    if (string.IsNullOrWhiteSpace(cadenaConexion))
        throw new InvalidOperationException(
            "Falta la cadena de conexión. Defínela como DB_CONNECTION en ApiPlantamoss/.env " +
            "o en la variable de entorno ConnectionStrings__Default.");
    builder.Services.AddDbContext<AppDbContext>(opt => opt.UseNpgsql(cadenaConexion));

    // -----------------------------------------------------------------
    // 6. Servicios propios
    // -----------------------------------------------------------------
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddMemoryCache();
    builder.Services.AddScoped<TokenService>();
    builder.Services.AddScoped<SesionService>();
    builder.Services.AddScoped<AuditoriaService>();
    builder.Services.AddScoped<CodigoService>();
    builder.Services.AddScoped<EmailService>();
    builder.Services.AddScoped<ClimaService>();
    builder.Services.AddScoped<YouTubeService>();
    builder.Services.AddScoped<RachaService>();
    builder.Services.AddScoped<InsigniasService>();
    builder.Services.AddSingleton<LoginIntentosService>();

    // Clientes HTTP para los servicios externos (IHttpClientFactory reutiliza conexiones)
    builder.Services.AddHttpClient(ClimaService.NombreHttpClient, c =>
    {
        c.BaseAddress = new Uri("https://api.openweathermap.org/");
        c.Timeout = TimeSpan.FromSeconds(10);
    });
    builder.Services.AddHttpClient(YouTubeService.NombreHttpClient, c =>
    {
        c.BaseAddress = new Uri("https://www.googleapis.com/");
        c.Timeout = TimeSpan.FromSeconds(10);
    });

    // -----------------------------------------------------------------
    // 7. Controladores + saneamiento de entradas + validación → 422 { message, errors } (claves camelCase)
    // -----------------------------------------------------------------
    builder.Services
        .AddControllers(opt =>
        {
            // Trim de textos y rechazo de caracteres de control en TODOS los DTOs
            opt.Filters.Add<SanitizarEntradasFilter>();
            // Solo cuentan los [Required] que escribimos nosotros (con mensaje en español)
            opt.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
            // Las claves de ModelState usan el nombre JSON (camelCase) de la propiedad
            opt.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider());
            // Límite de errores de validación acumulados (evita respuestas enormes)
            opt.MaxModelValidationErrors = 50;
        })
        .AddJsonOptions(o =>
        {
            // JSON muy anidado = posible ataque de denegación de servicio
            o.JsonSerializerOptions.MaxDepth = 32;
        })
        .ConfigureApiBehaviorOptions(opt =>
        {
            opt.InvalidModelStateResponseFactory = ctx =>
            {
                var errores = new ErroresValidacion();
                // Nombres de los parámetros de las acciones (ej. "req"): ese error significa "body vacío o JSON roto"
                var parametros = ctx.ActionDescriptor.Parameters
                    .Where(p => p.BindingInfo?.BindingSource == Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource.Body
                                || p.ParameterType.Namespace == "PlantamossApi.DTOs")
                    .Select(p => p.Name).ToHashSet();

                foreach (var (clave, entrada) in ctx.ModelState)
                {
                    if (entrada.Errors.Count == 0) continue;
                    var campo = Validaciones.ACamelCase(clave);
                    var esBody = campo == "" || parametros.Contains(clave);

                    foreach (var error in entrada.Errors)
                    {
                        var mensaje = error.ErrorMessage;
                        // Los mensajes del framework vienen en inglés: los traducimos a algo útil
                        var esDelFramework = string.IsNullOrEmpty(mensaje) || mensaje.StartsWith("The ")
                            || mensaje.Contains("JSON") || mensaje.StartsWith("A non-empty request body");

                        if (esBody)
                            errores.Agregar("body", "El cuerpo de la petición está vacío o no es un JSON válido.");
                        else
                            errores.Agregar(campo, esDelFramework ? "El valor de este campo no es válido." : mensaje);
                    }
                }

                return new ObjectResult(errores.ComoApiError()) { StatusCode = StatusCodes.Status422UnprocessableEntity };
            };
        });

    // -----------------------------------------------------------------
    // 8. Autenticación JWT + autorización por roles
    // -----------------------------------------------------------------
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(opt =>
        {
            // Conservamos los nombres cortos del JWT (sub, email, name, role) sin renombrarlos
            opt.MapInboundClaims = false;
            // En producción el token solo viaja por HTTPS
            opt.RequireHttpsMetadata = !esDesarrollo;
            opt.TokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = jwtOpciones.Issuer,
                ValidAudience = jwtOpciones.Audience,
                IssuerSigningKey = jwtOpciones.ClaveFirma(),
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                RequireExpirationTime = true,
                ClockSkew = TimeSpan.FromSeconds(30),
                NameClaimType = "name",
                // El claim "role" es el que usa [Authorize(Roles = "...")] y User.IsInRole(...)
                RoleClaimType = "role",
            };

            opt.Events = new JwtBearerEvents
            {
                // El token deja de servir aunque no haya expirado si: el usuario fue eliminado, está suspendido
                // o cambió/restableció su contraseña (el claim "stamp" ya no coincide).
                OnTokenValidated = async ctx =>
                {
                    var sub = ctx.Principal?.FindFirst("sub")?.Value;
                    var sello = ctx.Principal?.FindFirst(TokenService.ClaimSello)?.Value;
                    var db = ctx.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                    var datos = long.TryParse(sub, out var id)
                        ? await db.Users.Where(u => u.Id == id).Select(u => new { u.Estado, u.Password }).FirstOrDefaultAsync()
                        : null;

                    if (datos is null) ctx.Fail("El usuario ya no existe.");
                    else if (datos.Estado == EstadosCuenta.Suspendido) ctx.Fail("La cuenta está suspendida.");
                    else if (sello != TokenService.Sello(datos.Password)) ctx.Fail("La sesión fue cerrada.");
                },
                // 401 → JSON { message }
                OnChallenge = async ctx =>
                {
                    ctx.HandleResponse();
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    var mensaje = ctx.AuthenticateFailure?.Message switch
                    {
                        "La cuenta está suspendida." => "Tu cuenta está suspendida. Contacta al administrador.",
                        _ => "No has iniciado sesión o tu sesión expiró.",
                    };
                    await ctx.Response.WriteAsJsonAsync(new ApiError(mensaje));
                },
                // 403 → JSON { message }
                OnForbidden = async ctx =>
                {
                    ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await ctx.Response.WriteAsJsonAsync(new ApiError("No tienes permiso para realizar esta acción."));
                },
            };
        });
    builder.Services.AddAuthorization();

    // -----------------------------------------------------------------
    // 9. Rate limiting (429 con { message })
    // -----------------------------------------------------------------
    builder.Services.AddRateLimiter(opt =>
    {
        opt.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        opt.OnRejected = async (ctx, token) =>
        {
            ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await ctx.HttpContext.Response.WriteAsJsonAsync(
                new ApiError("Demasiados intentos. Espera un momento e inténtalo de nuevo."), token);
        };

        static string Ip(HttpContext http) => ClienteHttp.Ip(http) ?? "desconocida";

        // Login: máximo 20 peticiones por minuto por IP (además: bloqueo temporal por correo → LoginIntentosService)
        opt.AddPolicy("login", http => RateLimitPartition.GetFixedWindowLimiter(Ip(http), _ =>
            new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));

        // Registro (también envía un código de verificación): 10 por minuto por IP
        opt.AddPolicy("registro", http => RateLimitPartition.GetFixedWindowLimiter(Ip(http), _ =>
            new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));

        // Envío de códigos por correo (reenviar verificación, recuperar contraseña): 5 por minuto por IP
        opt.AddPolicy("codigos", http => RateLimitPartition.GetFixedWindowLimiter(Ip(http), _ =>
            new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));

        // Verificar códigos de 6 dígitos (fuerza bruta): 10 por minuto por IP (además, 5 intentos por código)
        opt.AddPolicy("verificar-codigo", http => RateLimitPartition.GetFixedWindowLimiter(Ip(http), _ =>
            new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));

        // Refresh / logout: 30 por minuto por IP
        opt.AddPolicy("refresh", http => RateLimitPartition.GetFixedWindowLimiter(Ip(http), _ =>
            new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    });

    // -----------------------------------------------------------------
    // 10. CORS. Producción: SOLO los orígenes de Cors:Origins.
    //     Desarrollo: además cualquier http://localhost:<puerto> (Flutter web usa un puerto aleatorio).
    //     La app móvil nativa (Flutter Android/iOS) no usa CORS.
    // -----------------------------------------------------------------
    var origenes = config.GetSection("Cors:Origins").Get<string[]>() ?? [];
    builder.Services.AddCors(opt => opt.AddDefaultPolicy(p =>
        p.SetIsOriginAllowed(origen =>
                origenes.Contains(origen) ||
                (esDesarrollo && Uri.TryCreate(origen, UriKind.Absolute, out var uri) && uri.Host is "localhost" or "127.0.0.1"))
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders(CorrelacionMiddleware.Header)
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10))));

    // -----------------------------------------------------------------
    // 11. Swagger: SOLO en Development (o con Swagger:Habilitado=true). En producción no se expone.
    // -----------------------------------------------------------------
    var swaggerHabilitado = esDesarrollo || config.GetValue<bool>("Swagger:Habilitado");
    if (swaggerHabilitado)
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Plantamoss API",
                Version = "v1",
                Description = "API REST del catálogo de flora mexicana Plantamoss.",
            });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Pega aquí solo el token (sin la palabra Bearer).",
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
                    Array.Empty<string>()
                },
            });
        });
    }

    // =================================================================
    //  Pipeline HTTP (el orden importa)
    // =================================================================
    var app = builder.Build();

    // IP y esquema reales cuando hay proxy (debe ir primero)
    app.UseForwardedHeaders();

    // X-Correlation-Id en la respuesta y en todos los logs de la petición
    app.UseMiddleware<CorrelacionMiddleware>();

    // Una línea de log por petición: método, ruta (SIN query string), status, ms, usuario e IP.
    // No se registran cuerpos, headers (Authorization) ni la query.
    app.UseSerilogRequestLogging(o =>
    {
        o.MessageTemplate = "HTTP {RequestMethod} {RequestPath} respondió {StatusCode} en {Elapsed:0.0} ms (usuario {UserId}, IP {ClientIp})";
        o.IncludeQueryInRequestPath = false;
        o.GetLevel = (http, _, ex) =>
            ex is not null || http.Response.StatusCode >= 500 ? LogEventLevel.Error
            : http.Response.StatusCode >= 400 ? LogEventLevel.Warning
            : LogEventLevel.Information;
        o.EnrichDiagnosticContext = (diag, http) =>
        {
            diag.Set("UserId", http.User.FindFirst("sub")?.Value ?? "anonimo");
            diag.Set("ClientIp", ClienteHttp.Ip(http) ?? "desconocida");
            diag.Set("UserAgent", ClienteHttp.UserAgent(http) ?? "");
        };
    });

    // Errores no controlados → 500 { message, traceId }
    app.UseMiddleware<ExcepcionesMiddleware>();

    // CORS lo más arriba posible: así TODAS las respuestas (413, 400 por HTTP, 404, 401, 422, 500...)
    // llevan Access-Control-Allow-Origin y el navegador muestra el error real en vez de "error de CORS".
    // También responde aquí los preflight OPTIONS antes de cualquier otra validación.
    app.UseCors();

    // Cabeceras de seguridad en todas las respuestas
    app.UseMiddleware<CabecerasSeguridadMiddleware>();

    // Cuerpo demasiado grande → 413 inmediato (sin leerlo). Kestrel además corta los envíos "chunked".
    app.Use(async (ctx, next) =>
    {
        if (ctx.Request.ContentLength > seguridad.MaxBodyBytes)
        {
            ctx.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            await ctx.Response.WriteAsJsonAsync(new ApiError(
                $"El cuerpo de la petición es demasiado grande (máximo {seguridad.MaxBodyBytes / 1024} KB)."));
            return;
        }
        await next();
    });

    // HTTPS: fuera de Development → HSTS + redirección de HTTP a HTTPS
    if (!esDesarrollo)
    {
        app.UseHsts();
        app.UseHttpsRedirection();
    }

    // Seguridad:RequireHttps=true (producción) → toda petición que no llegue por HTTPS se rechaza
    // (excepto /api/health, que usan las sondas del balanceador).
    if (seguridad.RequireHttps)
    {
        app.Use(async (ctx, next) =>
        {
            if (!ctx.Request.IsHttps && !ctx.Request.Path.StartsWithSegments("/api/health"))
            {
                ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                await ctx.Response.WriteAsJsonAsync(new ApiError("Esta API solo acepta conexiones HTTPS."));
                return;
            }
            await next();
        });
    }

    // Respuestas vacías 404/405/415 del framework → JSON { message }
    app.UseStatusCodePages(async ctx =>
    {
        var http = ctx.HttpContext;
        var mensaje = http.Response.StatusCode switch
        {
            404 => "Recurso no encontrado.",
            405 => "Método HTTP no permitido para esta ruta.",
            413 => "El cuerpo de la petición es demasiado grande.",
            415 => "El cuerpo debe enviarse como JSON (Content-Type: application/json).",
            _ => "No se pudo procesar la petición.",
        };
        await http.Response.WriteAsJsonAsync(new ApiError(mensaje));
    });

    if (swaggerHabilitado)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Plantamoss API v1");
            c.DocumentTitle = "Plantamoss API";
        });
    }

    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();

    app.MapControllers();
    if (swaggerHabilitado)
        app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
    app.MapGet("/api/health", () => Results.Ok(new { status = "ok" })).ExcludeFromDescription();

    Log.Information("Plantamoss API iniciando en entorno {Entorno} (Swagger {Swagger}, RequireHttps {Https})",
        app.Environment.EnvironmentName, swaggerHabilitado, seguridad.RequireHttps);
    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "La API no pudo arrancar: {Mensaje}", ex.Message);
    throw;
}
finally
{
    Log.CloseAndFlush();
}
