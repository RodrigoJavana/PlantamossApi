using Microsoft.EntityFrameworkCore;
using PlantamossApi.Models;

namespace PlantamossApi.Data;

/// <summary>
/// Contexto de Entity Framework Core.
/// IMPORTANTE: el esquema lo crea el script SQL de <c>database/init</c>; aquí NO hay migraciones
/// ni EnsureCreated. Solo mapeamos las clases C# (PascalCase) a las tablas/columnas snake_case.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Estado> Estados => Set<Estado>();
    public DbSet<Planta> Plantas => Set<Planta>();
    public DbSet<Noticia> Noticias => Set<Noticia>();
    public DbSet<Miniblog> Miniblogs => Set<Miniblog>();
    public DbSet<PasswordResetCode> PasswordResetCodes => Set<PasswordResetCode>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<BitacoraAuditoria> Bitacora => Set<BitacoraAuditoria>();
    public DbSet<Racha> Rachas => Set<Racha>();
    public DbSet<EventoUsuario> EventosUsuario => Set<EventoUsuario>();
    public DbSet<InsigniaUsuario> InsigniasUsuario => Set<InsigniaUsuario>();

    /// <summary>
    /// Función <c>unaccent()</c> de PostgreSQL (extensión "unaccent").
    /// Solo se puede usar dentro de consultas LINQ: EF la traduce a SQL.
    /// </summary>
    public static string Unaccent(string texto) =>
        throw new NotSupportedException("Unaccent solo se puede usar dentro de una consulta de EF Core.");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Registrar unaccent(text) para poder usarla en LINQ
        modelBuilder.HasDbFunction(typeof(AppDbContext).GetMethod(nameof(Unaccent), [typeof(string)])!)
            .HasName("unaccent")
            .IsBuiltIn();

        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(255);
            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(255);
            e.Property(x => x.EmailVerifiedAt).HasColumnName("email_verified_at");
            e.Property(x => x.Password).HasColumnName("password").HasMaxLength(255);
            e.Property(x => x.Role).HasColumnName("role").HasMaxLength(20);
            e.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(20);
            e.Property(x => x.CiudadesClima).HasColumnName("ciudades_clima");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<Estado>(e =>
        {
            e.ToTable("estados");
            e.HasKey(x => x.IdEstado);
            e.Property(x => x.IdEstado).HasColumnName("id_estado");
            e.Property(x => x.NombreEstado).HasColumnName("nombre_estado").HasMaxLength(100);
            e.Property(x => x.ClimaPredominante).HasColumnName("clima_predominante").HasMaxLength(100);
        });

        modelBuilder.Entity<Planta>(e =>
        {
            e.ToTable("catalogo_flora");
            e.HasKey(x => x.IdPlanta);
            e.Property(x => x.IdPlanta).HasColumnName("id_planta");
            e.Property(x => x.NombrePlanta).HasColumnName("nombre_planta").HasMaxLength(255);
            e.Property(x => x.NombreCientifico).HasColumnName("nombre_cientifico").HasMaxLength(255);
            e.Property(x => x.LetraInicial).HasColumnName("letra_inicial").HasColumnType("char(1)");
            e.Property(x => x.IdEstado).HasColumnName("id_estado");
            e.Property(x => x.UrlImagen).HasColumnName("url_imagen").HasMaxLength(500);
            e.Property(x => x.TipoClima).HasColumnName("tipo_clima").HasMaxLength(50);
            e.Property(x => x.LuzSol).HasColumnName("luz_sol").HasMaxLength(50);
            e.Property(x => x.Riego).HasColumnName("riego").HasMaxLength(100);
            e.Property(x => x.Descripcion).HasColumnName("descripcion");
            e.Property(x => x.CuidadosTexto).HasColumnName("cuidados_texto");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            // FK a estados (ON DELETE SET NULL en la BD)
            e.HasOne(x => x.Estado)
             .WithMany(s => s.Plantas)
             .HasForeignKey(x => x.IdEstado)
             .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Noticia>(e =>
        {
            e.ToTable("noticias");
            e.HasKey(x => x.IdNoticia);
            e.Property(x => x.IdNoticia).HasColumnName("id_noticia");
            e.Property(x => x.Titulo).HasColumnName("titulo").HasMaxLength(255);
            e.Property(x => x.Contenido).HasColumnName("contenido");
            e.Property(x => x.UrlImagen).HasColumnName("url_imagen").HasMaxLength(500);
            e.Property(x => x.UrlExterna).HasColumnName("url_externa").HasMaxLength(500);
            e.Property(x => x.Destacada).HasColumnName("destacada");
            e.Property(x => x.Activa).HasColumnName("activa");
            e.Property(x => x.IdEditor).HasColumnName("id_editor");
            e.Property(x => x.FechaPublicacion).HasColumnName("fecha_publicacion");

            e.HasOne(x => x.Editor)
             .WithMany()
             .HasForeignKey(x => x.IdEditor)
             .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Miniblog>(e =>
        {
            e.ToTable("miniblogs");
            e.HasKey(x => x.IdBlog);
            e.Property(x => x.IdBlog).HasColumnName("id_blog");
            e.Property(x => x.IdUsuario).HasColumnName("id_usuario");
            e.Property(x => x.Titulo).HasColumnName("titulo").HasMaxLength(255);
            e.Property(x => x.Contenido).HasColumnName("contenido");
            e.Property(x => x.Contenido2).HasColumnName("contenido2");
            e.Property(x => x.Contenido3).HasColumnName("contenido3");
            e.Property(x => x.UrlImagen).HasColumnName("url_imagen").HasMaxLength(500);
            e.Property(x => x.UrlImagen2).HasColumnName("url_imagen_2").HasMaxLength(500);
            e.Property(x => x.UrlImagen3).HasColumnName("url_imagen_3").HasMaxLength(500);
            e.Property(x => x.UrlImagen4).HasColumnName("url_imagen_4").HasMaxLength(500);
            e.Property(x => x.UrlImagen5).HasColumnName("url_imagen_5").HasMaxLength(500);
            e.Property(x => x.FechaPublicacion).HasColumnName("fecha_publicacion");

            e.HasOne(x => x.Usuario)
             .WithMany()
             .HasForeignKey(x => x.IdUsuario)
             .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PasswordResetCode>(e =>
        {
            e.ToTable("password_reset_codes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(255);
            // Se guarda el HMAC-SHA256 (hex) del código, nunca el código en claro
            e.Property(x => x.Code).HasColumnName("code").HasMaxLength(64);
            e.Property(x => x.Tipo).HasColumnName("tipo").HasMaxLength(20);
            e.Property(x => x.Intentos).HasColumnName("intentos");
            e.Property(x => x.ExpiresAt).HasColumnName("expires_at");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.ToTable("refresh_tokens");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.TokenHash).HasColumnName("token_hash").HasColumnType("char(64)");
            e.Property(x => x.ExpiraEn).HasColumnName("expira_en");
            e.Property(x => x.CreadoEn).HasColumnName("creado_en");
            e.Property(x => x.RevocadoEn).HasColumnName("revocado_en");
            e.Property(x => x.ReemplazadoPor).HasColumnName("reemplazado_por").HasColumnType("char(64)");
            e.Property(x => x.Ip).HasColumnName("ip").HasMaxLength(45);
            e.Property(x => x.UserAgent).HasColumnName("user_agent").HasMaxLength(300);
            e.HasIndex(x => x.TokenHash).IsUnique();

            e.HasOne(x => x.Usuario)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BitacoraAuditoria>(e =>
        {
            e.ToTable("bitacora_auditoria");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Fecha).HasColumnName("fecha");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(255);
            e.Property(x => x.Accion).HasColumnName("accion").HasMaxLength(60);
            e.Property(x => x.Detalle).HasColumnName("detalle").HasMaxLength(500);
            e.Property(x => x.Ip).HasColumnName("ip").HasMaxLength(45);
            e.Property(x => x.Exito).HasColumnName("exito");
            // FK a users ON DELETE SET NULL (se conserva el registro aunque se borre el usuario)
            e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Racha>(e =>
        {
            e.ToTable("rachas");
            // La PK es el propio user_id (una fila por usuario)
            e.HasKey(x => x.UserId);
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.RachaActual).HasColumnName("racha_actual");
            e.Property(x => x.RachaMaxima).HasColumnName("racha_maxima");
            e.Property(x => x.TotalDias).HasColumnName("total_dias");
            e.Property(x => x.UltimoDia).HasColumnName("ultimo_dia").HasColumnType("date");
            e.Property(x => x.ActualizadoEn).HasColumnName("actualizado_en");

            // FK a users ON DELETE CASCADE (al borrar la cuenta se borra su racha)
            e.HasOne(x => x.Usuario)
             .WithOne()
             .HasForeignKey<Racha>(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EventoUsuario>(e =>
        {
            e.ToTable("eventos_usuario");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.Evento).HasColumnName("evento").HasMaxLength(30);
            e.Property(x => x.Referencia).HasColumnName("referencia").HasMaxLength(64);
            e.Property(x => x.CreadoEn).HasColumnName("creado_en");

            // FK a users ON DELETE CASCADE (al borrar la cuenta se borran sus eventos)
            e.HasOne(x => x.Usuario)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InsigniaUsuario>(e =>
        {
            e.ToTable("insignias_usuario");
            // PK compuesta: un usuario desbloquea cada insignia una sola vez
            e.HasKey(x => new { x.UserId, x.Codigo });
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.Codigo).HasColumnName("codigo").HasMaxLength(40);
            e.Property(x => x.DesbloqueadaEn).HasColumnName("desbloqueada_en");

            // FK a users ON DELETE CASCADE (al borrar la cuenta se borran sus insignias)
            e.HasOne(x => x.Usuario)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
