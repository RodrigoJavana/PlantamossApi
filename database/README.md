# Plantamoss 2026 - Base de datos (PostgreSQL 16)

Imagen propia basada en `postgres:16-alpine` (zona horaria `America/Monterrey`, UTF8).

```bash
docker build -t plantamoss-db ./database
docker run -d --name plantamoss-db -p 5434:5432 plantamoss-db
```

Conexión: `Host=localhost;Port=5434;Database=plantamoss;Username=plantamoss;Password=plantamoss_dev`

- `init/01_schema.sql`: extensiones (`pgcrypto`, `unaccent`), tablas, CHECKs, índices y triggers (`updated_at`, `letra_inicial` de respaldo).
- `init/02_seed.sql`: 32 estados, 31 plantas, 4 usuarios, 4 noticias, 3 miniblogs.
- `conf/plantamoss.conf`: afinación ligera (log de consultas > 500 ms).

Los scripts de `init/` solo corren cuando el volumen de datos está vacío
(para re-sembrar: `docker compose down -v`).

## Usuarios de prueba (solo desarrollo)

| Correo | Rol | Contraseña |
|---|---|---|
| admin@plantamoss.com | admin | `Admin123!` |
| editor@plantamoss.com | editor | `Editor123!` |
| noticias@plantamoss.com | noticias | `Noticias123!` |
| usuario@plantamoss.com | user | `Usuario123!` |
