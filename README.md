# Plantamoss API (.NET 9 + PostgreSQL 16)

API REST del catálogo de flora mexicana **Plantamoss** (migración del proyecto Laravel original).
La consume el front Vue 3 (`Plantamoss2026`, puerto 5173). El contrato completo de rutas y DTOs está en
`Plantamoss2026/CONTRATO.md`.

```
ApiPlantamoss/
├── ApiPlantamoss.sln
├── docker-compose.yml        # db (PostgreSQL) + api (+ pgadmin opcional)
├── .env.example              # variables para docker compose (copiar a .env)
├── database/                 # imagen de PostgreSQL con esquema y datos semilla (init/*.sql)
└── PlantamossApi/
    ├── Program.cs            # configuración: BD, JWT, CORS, rate limiting, Swagger, pipeline
    ├── Controllers/          # Auth, Perfil, Estados, Plantas, Noticias, EditorNoticias, Blogs, Admin, Estadisticas, Externo
    ├── Data/AppDbContext.cs  # mapeo de entidades a tablas snake_case + función unaccent()
    ├── Models/               # entidades (User, Estado, Planta, Noticia, Miniblog, PasswordResetCode)
    ├── DTOs/                 # objetos de entrada/salida (JSON camelCase) con sus validaciones
    ├── Helpers/              # validaciones, atributos de validación propios, mapeos a DTO
    ├── Services/             # TokenService, EmailService, CodigoService, ClimaService, YouTubeService, LoginIntentosService
    ├── Middleware/           # ExcepcionesMiddleware (500 → { message })
    └── Dockerfile
```

## Requisitos

* .NET SDK 9 (`dotnet --list-sdks` debe mostrar 9.0.x)
* Docker Desktop (para PostgreSQL)

## Cómo levantarlo

### Opción A — BD en Docker + API con `dotnet run` (recomendada para desarrollar)

```bash
cd ApiPlantamoss
docker compose up -d db          # PostgreSQL en localhost:5434 (crea el esquema y el seed la primera vez)
cd PlantamossApi
dotnet run                       # API en http://localhost:5080  ·  Swagger en http://localhost:5080/swagger
```

`appsettings.Development.json` ya trae la cadena de conexión local:
`Host=localhost;Port=5434;Database=plantamoss;Username=plantamoss;Password=plantamoss_dev`.

### Opción B — Todo con Docker Compose

```bash
cd ApiPlantamoss
cp .env.example .env             # (opcional) llena tus API keys / SMTP
docker compose up -d --build     # db + api  →  http://localhost:5080/swagger
docker compose --profile tools up -d   # además pgAdmin en http://localhost:5050
```

Dentro de compose la API se conecta a `db:5432`. Las API keys de OpenWeather/YouTube y el SMTP se pasan con
variables del `.env` (`OPENWEATHER_API_KEY`, `YOUTUBE_API_KEY`, `SMTP_*`, `JWT_KEY`).

### Reiniciar la base de datos desde cero

```bash
docker compose down
docker volume rm plantamoss_pgdata
docker compose up -d --build db
```

> El esquema lo crean los scripts de `database/init/` (solo cuando el volumen está vacío).
> La API **no** usa migraciones de EF ni `EnsureCreated`.

## Usuarios de prueba (seed)

| Correo | Contraseña | Rol |
|---|---|---|
| admin@plantamoss.com | `Admin123!` | admin |
| editor@plantamoss.com | `Editor123!` | editor (catálogo de plantas) |
| noticias@plantamoss.com | `Noticias123!` | noticias (noticias y moderación de blogs) |
| usuario@plantamoss.com | `Usuario123!` | user |

Todos tienen el correo ya verificado.

## Endpoints (base `http://localhost:5080/api`)

Errores siempre con forma `{ "message": "...", "errors": { "campo": ["..."] } }` (`errors` solo en 422).
Códigos: 400 regla de negocio · 401 sin sesión · 403 sin permiso / cuenta suspendida / correo sin verificar ·
404 no existe · 422 validación · 429 demasiados intentos · 500 error inesperado.

| Método | Ruta | Acceso | Descripción |
|---|---|---|---|
| POST | `/auth/register` | público | Registro (envía código de verificación) → 201 `{ token, user }` |
| POST | `/auth/login` | público | Login → `{ token, user }` (429 tras 5 fallos/min por correo) |
| GET | `/auth/me` | auth | Usuario actual |
| POST | `/auth/verificar-correo` | auth | `{ codigo }` verifica el correo |
| POST | `/auth/verificar-correo/reenviar` | auth | Reenvía el código |
| POST | `/auth/recuperar` | público | `{ email }` envía código de recuperación (15 min) |
| POST | `/auth/recuperar/verificar` | público | `{ email, codigo }` → `{ resetToken }` (máx. 5 intentos) |
| POST | `/auth/recuperar/restablecer` | público | `{ email, resetToken, password, passwordConfirmation }` |
| PUT | `/perfil` | auth | `{ name }` |
| PUT | `/perfil/password` | auth | `{ passwordActual, password, passwordConfirmation }` |
| DELETE | `/perfil` | auth | `{ password }` elimina la cuenta → 204 |
| GET | `/noticias/recientes` | público | Máx. 4 noticias activas (destacadas primero) |
| GET | `/estados` | público | Estados con `cantidadPlantas` |
| GET | `/estados/{id}/plantas` | público | `{ estado, plantas }` |
| GET | `/plantas?q=&estado=&clima=&luz=` | público | Búsqueda sin distinguir mayúsculas ni acentos (`unaccent`) |
| GET | `/plantas/{id}` | público | Detalle |
| POST/PUT/DELETE | `/plantas`, `/plantas/{id}` | editor, admin | CRUD del catálogo |
| GET | `/blogs` | público | Blogs (fecha desc) |
| GET | `/blogs/{id}` | público | Detalle de un blog |
| GET | `/blogs/mios` | auth | Blogs del usuario actual |
| POST | `/blogs` | auth + correo verificado | Publicar blog → 201 (403 "Verifica tu correo para publicar.") |
| PUT/DELETE | `/blogs/{id}` | dueño, noticias, admin | Editar / eliminar blog |
| GET/POST | `/editor/noticias` | noticias, admin | Listar todas / crear |
| PUT/DELETE | `/editor/noticias/{id}` | noticias, admin | Editar / eliminar |
| PATCH | `/editor/noticias/{id}/toggle` | noticias, admin | Activar / desactivar |
| GET | `/admin/usuarios?q=` | admin | Lista de usuarios |
| GET/PUT/DELETE | `/admin/usuarios/{id}` | admin | Detalle / editar / eliminar (no puedes borrarte a ti mismo) |
| POST | `/admin/usuarios` | admin | Crear usuario (correo ya verificado) |
| GET | `/admin/estadisticas` | admin, editor, noticias | Totales para el dashboard del panel |
| GET | `/externo/clima?q=Monterrey,MX` | público | Clima (OpenWeather, caché 10 min, `ok=false` si falla) |
| GET | `/externo/videos` | público | 6 videos de YouTube (caché 6 h, `[]` si falla) |

Swagger: `http://localhost:5080/swagger` → botón **Authorize** → pega el token (sin la palabra `Bearer`).

## Correo (SMTP)

Sin SMTP configurado (`Smtp:Host` vacío, el valor por defecto) **los códigos no se envían**: se escriben en la
consola de la API con nivel Warning, en un bloque así:

```
==================== CORREO (SMTP no configurado) ====================
  Para: alguien@correo.com
  Asunto: Verifica tu correo - Plantamoss
  CÓDIGO (verificacion): 123456
======================================================================
```

Para enviar correos reales con Gmail, usa **user-secrets** (no subas la contraseña a git):

```bash
cd PlantamossApi
dotnet user-secrets init
dotnet user-secrets set "Smtp:Host" "smtp.gmail.com"
dotnet user-secrets set "Smtp:Port" "465"
dotnet user-secrets set "Smtp:EnableSsl" "true"
dotnet user-secrets set "Smtp:User" "tucuenta@gmail.com"
dotnet user-secrets set "Smtp:Password" "xxxx xxxx xxxx xxxx"   # contraseña de aplicación de Google
dotnet user-secrets set "Smtp:From" "tucuenta@gmail.com"
```

La contraseña debe ser una *contraseña de aplicación* (Cuenta de Google → Seguridad → Verificación en dos pasos →
Contraseñas de aplicaciones). Puerto 465 usa SSL directo; con 587 se usa STARTTLS automáticamente.
Con Docker Compose usa las variables `SMTP_HOST`, `SMTP_PORT`, `SMTP_USER`, `SMTP_PASSWORD`, `SMTP_FROM` del `.env`.

## Seguridad — pendientes importantes

* **API keys externas:** `appsettings.Development.json` trae las keys de OpenWeather y YouTube que el proyecto
  original tenía expuestas en su JavaScript. Ya son públicas: **rótalas** (genera nuevas en la consola de cada
  servicio) y muévelas a user-secrets o variables de entorno:
  ```bash
  dotnet user-secrets set "OpenWeather:ApiKey" "<nueva>"
  dotnet user-secrets set "YouTube:ApiKey" "<nueva>"
  ```
  y bórralas de `appsettings.Development.json`. Ahora solo viven en el servidor (el front llama a `/api/externo/*`).
* **Jwt:Key:** la de desarrollo es pública; en cualquier despliegue define `Jwt__Key` (≥ 32 caracteres aleatorios).
* Swagger está habilitado siempre (proyecto escolar); en producción conviene limitarlo a Development.

## Notas de implementación

* **Validación:** los DTOs usan atributos (`[Required]`, `[PasswordFuerte]`, `[UrlHttp]`, `[ValoresPermitidos]`,
  `[Compare]`, `[DebeSerVerdadero]`…) y todos los errores de formato salen juntos en un 422. Las reglas que
  necesitan la BD (correo repetido, estado inexistente, contraseña actual, código) se revisan después en el
  controlador, por lo que pueden llegar en un segundo 422.
* **Imágenes:** los campos `urlImagen*` aceptan URLs `http(s)://` **o** rutas del sitio que empiezan con `/`
  (como `/img/agave-azul.jpeg` del seed), para poder editar plantas del seed sin cambiar su imagen.
  `urlExterna` de noticias sí exige `http(s)://`.
* **Sesión:** el JWT dura 8 h. Si el usuario es eliminado o suspendido, su token deja de funcionar (401) aunque no
  haya expirado.
* **Recuperación:** el `resetToken` es un JWT de 15 min con `purpose=reset`; se invalida en cuanto la contraseña
  cambia (solo se puede usar una vez) y no sirve como token de sesión.
* **Rate limiting:** login 20 peticiones/min por IP + 5 fallos/min por correo; registro 10/min por IP;
  reenviar verificación y recuperar contraseña 5/min por IP.
* **Admin:** además de no poder eliminarse, el admin no puede quitarse su propio rol de admin ni suspenderse
  (400), para no quedarse fuera del sistema.
