<#
  Genera/actualiza ApiPlantamoss/.env (lo leen docker compose y `dotnet run`).

  - Toma el SMTP del .env del proyecto Laravel original (MAIL_HOST, MAIL_USERNAME, MAIL_PASSWORD...).
  - Usa las API keys que pases por parámetro; si no pasas nada conserva las que ya tenga el .env,
    y si tampoco hay, te las pide en consola (Enter = dejar vacía).
  - Si no existen JWT_KEY / PEPPER_CODIGOS genera valores aleatorios.

  Uso:
    .\configurar-env.ps1
    .\configurar-env.ps1 -OpenWeatherKey "xxxx" -YouTubeKey "yyyy"
    .\configurar-env.ps1 -LaravelEnv "D:\otra\ruta\.env"
#>
param(
    [string]$OpenWeatherKey,
    [string]$YouTubeKey,
    [string]$LaravelEnv = "$env:USERPROFILE\Desktop\Archivos\Planta-Moss-Proyecto\.env",
    [switch]$NoPreguntar
)

$ErrorActionPreference = 'Stop'
$destino = Join-Path $PSScriptRoot '.env'

function Leer-Env([string]$ruta) {
    $valores = [ordered]@{}
    if (Test-Path $ruta) {
        foreach ($linea in Get-Content $ruta -Encoding UTF8) {
            $t = $linea.Trim()
            if ($t -eq '' -or $t.StartsWith('#') -or -not $t.Contains('=')) { continue }
            $i = $t.IndexOf('=')
            $valores[$t.Substring(0, $i).Trim()] = $t.Substring($i + 1).Trim().Trim('"', "'")
        }
    }
    return $valores
}

$actual  = Leer-Env $destino
$laravel = Leer-Env $LaravelEnv
if ($laravel.Count -eq 0) { Write-Warning "No se encontró el .env de Laravel en: $LaravelEnv (el SMTP quedará como esté)." }

function Valor([string]$nuevo, [string]$clave, [string]$defecto = '') {
    if ($nuevo) { return $nuevo }
    if ($actual.Contains($clave) -and $actual[$clave]) { return $actual[$clave] }
    return $defecto
}

# --- API keys ---
$ow = Valor $OpenWeatherKey 'OPENWEATHER_API_KEY'
$yt = Valor $YouTubeKey 'YOUTUBE_API_KEY'
if (-not $NoPreguntar) {
    if (-not $ow) { $ow = Read-Host 'API key de OpenWeather (Enter para dejar vacía)' }
    if (-not $yt) { $yt = Read-Host 'API key de YouTube Data v3 (Enter para dejar vacía)' }
}

# --- SMTP (mismo correo que el proyecto Laravel) ---
$ssl = if ($laravel['MAIL_ENCRYPTION'] -in @('ssl', 'tls')) { 'true' } else { Valor '' 'SMTP_ENABLE_SSL' 'true' }
$smtpHost = Valor $laravel['MAIL_HOST'] 'SMTP_HOST'
$smtpPort = Valor $laravel['MAIL_PORT'] 'SMTP_PORT' '465'
$smtpUser = Valor $laravel['MAIL_USERNAME'] 'SMTP_USER'
$smtpPass = Valor $laravel['MAIL_PASSWORD'] 'SMTP_PASSWORD'
$smtpFrom = Valor $laravel['MAIL_FROM_ADDRESS'] 'SMTP_FROM' $smtpUser

# --- JWT ---
$jwt = $actual['JWT_KEY']
if (-not $jwt -or $jwt.Length -lt 32 -or $jwt -like 'cambia-esta-clave*') {
    $bytes = New-Object byte[] 48
    [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    $jwt = [Convert]::ToBase64String($bytes)
}

# --- Pepper de los códigos de 6 dígitos (HMAC) ---
$pepper = $actual['PEPPER_CODIGOS']
if (-not $pepper -or $pepper.Length -lt 32) {
    $bytes = New-Object byte[] 48
    [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    $pepper = [Convert]::ToBase64String($bytes)
}
$dbConexion = Valor '' 'DB_CONNECTION' 'Host=localhost;Port=5434;Database=plantamoss;Username=plantamoss;Password=plantamoss_dev'

$contenido = @"
# Generado por configurar-env.ps1 — NO subir a git (está en .gitignore).
# Lo leen: docker compose (servicio api) y `dotnet run` (Helpers/DotEnv.cs).

JWT_KEY=$jwt
CORS_ORIGIN=$(Valor '' 'CORS_ORIGIN' 'http://localhost:5173')

# Pepper secreto del HMAC de los códigos de 6 dígitos
PEPPER_CODIGOS=$pepper

# Cadena de conexión de PostgreSQL para `dotnet run`
DB_CONNECTION=$dbConexion

# Servicios externos (proxy del servidor)
OPENWEATHER_API_KEY=$ow
YOUTUBE_API_KEY=$yt

# SMTP (mismo correo que el proyecto Laravel). Gmail requiere "contraseña de aplicación".
SMTP_HOST=$smtpHost
SMTP_PORT=$smtpPort
SMTP_USER=$smtpUser
SMTP_PASSWORD=$smtpPass
SMTP_FROM=$smtpFrom
SMTP_ENABLE_SSL=$ssl

# pgAdmin (solo con: docker compose --profile tools up -d)
PGADMIN_EMAIL=$(Valor '' 'PGADMIN_EMAIL' 'admin@plantamoss.com')
PGADMIN_PASSWORD=$(Valor '' 'PGADMIN_PASSWORD' 'plantamoss_dev')
"@

Set-Content -Path $destino -Value $contenido -Encoding UTF8
Write-Host "Listo: $destino" -ForegroundColor Green
Write-Host ("  OpenWeather: {0} | YouTube: {1} | SMTP: {2} ({3})" -f `
    ($(if ($ow) { 'OK' } else { 'vacía' })), ($(if ($yt) { 'OK' } else { 'vacía' })), `
    ($(if ($smtpHost) { $smtpHost } else { 'sin configurar' })), ($(if ($smtpPass) { 'con contraseña' } else { 'SIN contraseña' })))
Write-Host "Reinicia la API (dotnet run / docker compose up -d --build api) para aplicar los cambios."
