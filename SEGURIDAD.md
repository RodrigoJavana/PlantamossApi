# Informe de Seguridad - API Plantamoss (.NET 9)
**Asignatura:** Seguridad en el Desarrollo de Software (Unidad II)  
**Proyecto:** API REST de Flora Mexicana Plantamoss  
**Tecnología:** .NET 9 Web API · PostgreSQL 16 · Entity Framework Core · Serilog  

---

## 1. Resumen Ejecutivo

El backend de **Plantamoss** ha sido implementado bajo el principio de **Defensa en Profundidad (Defense in Depth)** y siguiendo las directrices del **OWASP Top 10 API Security Risks**. Este documento detalla los controles técnicos, criptográficos y arquitectónicos implementados para garantizar la confidencialidad, integridad, autenticidad y disponibilidad del sistema.

---

## 2. Controles de Seguridad Implementados

### 2.1. Cifrado en Tránsito y HTTPS Estricto
- **Forzado de HTTPS:** En entornos productivos (`Seguridad:RequireHttps = true`), cualquier petición entrante por HTTP es rechazada de inmediato (`400 Bad Request`), a excepción del endpoint de sondeo de salud (`/api/health`).
- **HSTS (HTTP Strict Transport Security):** Se inyecta la cabecera `Strict-Transport-Security: max-age=31536000; includeSubDomains; preload` garantizando que los clientes web no puedan degradar la conexión a texto plano durante al menos 1 año.
- **Terminación TLS y Proxies Inversos:** Soporte nativo para terminación TLS en Azure App Service y Cloudflare mediante `UseForwardedHeaders` (`X-Forwarded-Proto` y `X-Forwarded-For`), evitando que el proxy transmita cabeceras falsificadas (`ForwardLimit = 1`).

### 2.2. Autenticación Robusta y Refresh Tokens con Rotación (OAuth 2.0 / JWT)
- **Tokens de Acceso de Corta Duración:** Tokens JWT (HMAC-SHA256) con expiración corta (60 minutos por defecto). Contienen identificador de usuario (`sub`), correo (`email`), nombre (`name`), rol (`role`), identificador único de token (`jti`) y un sello de seguridad criptográfico (`stamp`).
- **Invalidez Instantánea por Sello Criptográfico (`stamp`):** En `OnTokenValidated`, se verifica el `stamp` derivado del hash de la contraseña actual del usuario. Si el usuario cambia su contraseña, se suspende o se elimina, todos los tokens JWT emitidos previamente quedan invalidados de inmediato sin esperar su expiración natural.
- **Refresh Tokens Opacos:** Criptográficamente seguros generados con un generador de números pseudoaleatorios criptográfico (CSPRNG, 64 bytes).
- **Almacenamiento Cifrado en Base de Datos:** En la base de datos **nunca** se almacena el refresh token en texto plano, sino su hash `SHA-256` en la tabla `refresh_tokens`.
- **Rotación Estricta:** Cada vez que se utiliza `/api/auth/refresh`, el refresh token presentado es revocado atómicamente y se emite un nuevo par de credenciales (`token` y `refreshToken`).
- **Detección y Mitigación de Reuso de Tokens (Token Hijacking):** Si un refresh token revocado es presentado nuevamente (síntoma de robo o intercepción del token), el sistema detecta la anomalía, registra una alerta crítica de auditoría y revoca **inmediatamente todas las sesiones activas** del usuario comprometido.
- **Cierre de Sesión:**
  - `POST /api/auth/logout`: Revoca el token actual.
  - `POST /api/auth/logout-todo`: Revoca todas las sesiones en todos los dispositivos asociados al usuario.

### 2.3. Auditoría y Registro de Logs (Serilog + Base de Datos)
- **Registro en Archivos con Rotación Diaria:**
  - Archivo de texto estructurado: `PlantamossApi/logs/plantamoss-YYYYMMDD.log` para lectura operativa.
  - Archivo JSON compacto: `PlantamossApi/logs/plantamoss-YYYYMMDD.json` (`CompactJsonFormatter`) preparado para indexación en herramientas SIEM (Elasticsearch, Graylog, Azure Log Analytics).
  - Retención automática de 14 días.
- **Protección de Datos Sensibles (PII y Credenciales):**
  - **Nunca** se registran en los archivos de log cuerpos de peticiones (`Request Body`), contraseñas, tokens JWT, el encabezado `Authorization`, ni cadenas de consulta (`Query Strings`).
- **Trazabilidad mediante Correlación:**
  - Implementación de `CorrelacionMiddleware` que asigna un `X-Correlation-Id` a cada petición HTTP y lo asocia a todas las trazas de Serilog para rastreo forense de incidentes.
- **Bitácora de Auditoría en Base de Datos (`/api/admin/bitacora`):**
  - Tabla `bitacora_auditoria` con índices optimizados.
  - Registra eventos críticos: `LOGIN_OK`, `LOGIN_FALLIDO`, `LOGIN_BLOQUEADO`, `REFRESH_REUSO`, `REGISTRO`, `LOGOUT`, `PASSWORD_RESET`, `USUARIO_CREADO`, `USUARIO_EDITADO`, `USUARIO_ELIMINADO`.
  - Acceso restringido exclusivamente a usuarios con rol `admin` mediante autorización granular `[Authorize(Roles = "admin")]`.

### 2.4. Cabeceras de Seguridad y Mitigación de Ataques Web
El middleware `CabecerasSeguridadMiddleware` inyecta las siguientes cabeceras en todas las respuestas:
- `X-Content-Type-Options: nosniff` (previene ataques MIME-confusión).
- `X-Frame-Options: DENY` (inmunidad contra Clickjacking).
- `Referrer-Policy: strict-origin-when-cross-origin` (evita fuga de URLs internas a dominios externos).
- `Permissions-Policy: geolocation=(), camera=(), microphone=()` (bloquea el acceso a APIs de hardware innecesarias).
- `Content-Security-Policy: default-src 'self'; ...` (mitiga ataques XSS e inyección de recursos externos).
- **Supresión de Huellas del Servidor:** Se desactiva la cabecera `Server: Kestrel` en Kestrel para evitar divulgar la tecnología y versiones utilizadas.

### 2.5. Prevención de Ataques DoS y Fuerza Bruta
- **Límite Estricto de Carga Útil:** Kestrel y el pipeline HTTP rechazan cualquier petición que exceda **1 MB** (`MaxBodyBytes = 1,048,576`), retornando inmediatamente `413 Payload Too Large` sin procesar el contenido.
- **Rate Limiting (Límite de Tasa de Peticiones):**
  - Endpoint `/api/auth/login`: Máximo 20 peticiones/minuto por IP.
  - Endpoint `/api/auth/register`: Máximo 10 peticiones/minuto por IP.
  - Endpoint `/api/auth/recuperar` y reenvío de códigos: Máximo 5 peticiones/minuto por IP.
  - Endpoint de validación de códigos de 6 dígitos: Máximo 10 peticiones/minuto por IP.
  - Endpoint `/api/auth/refresh`: Máximo 30 peticiones/minuto por IP.
- **Bloqueo Temporal por Cuenta (`LoginIntentosService`):**
  - Tras 5 intentos fallidos consecutivos de inicio de sesión para un correo electrónico, la cuenta se bloquea automáticamente durante 15 minutos, mitigando ataques de fuerza bruta distribuidos.

### 2.6. Seguridad en Restablecimiento de Contraseñas y Códigos OTP
- **Criptografía de Códigos OTP:** Los códigos de 6 dígitos para verificación de correo o recuperación de cuenta **no se almacenan en texto plano**. Se protegen mediante un `HMAC-SHA256` utilizando una clave secreta adicional (*Pepper*) independiente del código.
- **Límite de Intentos y Caducidad:** Máximo 5 intentos de validación por código. Los códigos caducan automáticamente tras 15 minutos.
- **Tokens de Recuperación Desacoplados:** Tras validar el código de 6 dígitos, se expide un token temporal de recuperación firmado con propósito específico (`purpose: reset`) y audiencia restringida (`plantamoss-front-reset`), impidiendo que sea usado como credencial de sesión.

### 2.7. Prevención de Inyecciones (SQLi y Validación de Datos)
- **Entity Framework Core:** Todas las consultas a PostgreSQL están 100% parametrizadas a través de LINQ y EF Core. No existe concatenación de cadenas SQL en ninguna capa de la aplicación.
- **Saneamiento Automático de Entradas (`SanitizarEntradasFilter`):**
  - Se eliminan espacios en blanco al inicio y final (`trim`).
  - Se eliminan caracteres de control ASCII (`\0`, `\r`, `\b`, etc.) que pudieran provocar inyecciones de cabeceras o logs.
- **Validación Fuerte de DTOs:**
  - Contraseñas con requisitos estrictos de complejidad (mínimo 8 caracteres, mayúscula, minúscula, dígito y carácter especial).
  - Profundidad de deserialización JSON limitada a 32 niveles (`MaxDepth = 32`) para evitar ataques de desbordamiento de pila por JSON anidados.

### 2.8. Gestión Segura de Secretos y Repositorio Git
- **Aislamiento Total de Secretos:**
  - Los archivos de configuración versionados en Git (`appsettings.json`, `appsettings.Development.json`) tienen los valores de secretos (`Jwt:Key`, `PepperCodigos`, cadenas de conexión, SMTP y API keys) completamente vacíos.
  - Los secretos se cargan en tiempo de ejecución desde un archivo `.env` local (`DotEnv.cs`) o desde las variables de entorno del sistema operativo / contenedor.
- **Exclusiones en `.gitignore`:**
  - El archivo `.env`, copias de respaldo, carpetas de compilación (`bin/`, `obj/`), certificados (`*.pfx`, `*.pem`, `*.key`) y la carpeta de registros `PlantamossApi/logs` están estrictamente ignorados en Git.
  - Se provee `.env.example` con valores de plantilla para facilitar el despliegue sin exponer secretos.

---

## 3. Matriz de Cumplimiento OWASP API Security Top 10

| Riesgo OWASP API | Medida de Mitigación en Plantamoss API | Estado |
|---|---|---|
| **API1: Broken Object Level Authorization (BOLA)** | Verificación estricta de propiedad en `UsuarioId` sobre recursos de perfil y consultas controladas. | Cumplido |
| **API2: Broken Authentication** | JWT con firma HMAC-SHA256, Refresh Tokens con rotación, detección de reuso, BCrypt para contraseñas, bloqueo temporal de cuenta. | Cumplido |
| **API3: Broken Object Property Level Authorization** | DTOs específicos de entrada/salida; no se exponen modelos directos de base de datos (`UserDto`, `PlantasDto`). | Cumplido |
| **API4: Unrestricted Resource Consumption** | Rate Limiting por IP/endpoint, tamaño de cuerpo restringido a 1 MB, profundidad JSON limitada a 32, paginación obligatoria en bitácora. | Cumplido |
| **API5: Broken Function Level Authorization (BFLA)** | Decoradores `[Authorize(Roles = "admin")]` y `[Authorize(Roles = "admin,editor,noticias")]` en controladores administrativos. | Cumplido |
| **API6: Unrestricted Access to Sensitive Business Flows** | Tiempos de expiración breves, límite de intentos en códigos OTP y bloqueo anti-fuerza bruta. | Cumplido |
| **API7: Server Side Request Forgery (SSRF)** | URLs fijas y preconfiguradas en `IHttpClientFactory` hacia proveedores confiables (OpenWeather, YouTube). | Cumplido |
| **API8: Security Misconfiguration** | Cabeceras de seguridad (`CSP`, `HSTS`, `nosniff`, `DENY`), cabecera `Server` eliminada, Swagger desactivado en producción. | Cumplido |
| **API9: Improper Inventory Management** | Contrato de API documentado (`CONTRATO.md`), versionado claro de rutas `/api/...`. | Cumplido |
| **API10: Unsafe Consumption of APIs** | Validación de respuestas de APIs externas y timeouts explícitos de 10 segundos en `HttpClient`. | Cumplido |

---

## 4. Guía de Pruebas y Verificación

1. **Compilación y análisis estático:**
   ```bash
   dotnet build ApiPlantamoss.sln
   ```
2. **Verificación de secretos en Git:**
   ```bash
   git status
   # Comprobar que .env y logs/ no son rastreados
   ```
3. **Comprobación de cabeceras de seguridad:**
   ```bash
   curl -I http://localhost:5080/api/health
   ```
4. **Comprobación de límite de carga útil (413 Payload Too Large):**
   Envío de cuerpo superior a 1 MB genera error 413 inmediato.
5. **Comprobación de Rotación de Refresh Token:**
   - Autenticación en `POST /api/auth/login` devuelve `{ token, refreshToken }`.
   - Llamada a `POST /api/auth/refresh` con `refreshToken` devuelve un nuevo par y revoca el anterior.
   - Segunda llamada con el token anterior retorna `401 Unauthorized` y revoca todas las sesiones del usuario.
