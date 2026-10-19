<#
  Crea la base de datos de la API Plantamoss en Azure Database for PostgreSQL (servidor flexible)
  y genera el archivo de variables de entorno para el App Service.

  Qué hace:
    1. Te pide la contraseña del administrador del servidor (oculta; no se guarda en ningún lado).
    2. Crea un usuario propio de la app (mínimo privilegio) con contraseña aleatoria.
    3. Crea la base "plantamoss_api" (dueño: ese usuario) y las extensiones pgcrypto/unaccent.
    4. Carga database/init/01_schema.sql y 02_seed.sql conectado como el usuario de la app.
    5. Genera azure-appsettings.json (en .gitignore) con TODAS las variables para pegar en
       App Service > Variables de entorno > Edición avanzada.

  Requisitos: Docker Desktop encendido (usa la imagen postgres:17-alpine solo como cliente psql),
  tu IP permitida en el firewall del servidor y azure.extensions con PGCRYPTO y UNACCENT.

  Uso:
    .\desplegar-bd-azure.ps1
    .\desplegar-bd-azure.ps1 -Servidor plantamoss.postgres.database.azure.com -Admin rjavana
#>
param(
    [string]$Servidor = 'plantamoss.postgres.database.azure.com',
    [string]$Admin = 'rjavana',
    [string]$BaseDatos = 'plantamoss_api',
    [string]$UsuarioApp = 'plantamoss_app',
    [switch]$SoloVariables   # no toca la BD; solo regenera azure-appsettings.json
)

$ErrorActionPreference = 'Stop'
$raiz = $PSScriptRoot
$imagen = 'postgres:17-alpine'

function Leer-Env([string]$ruta) {
    $v = @{}
    if (Test-Path $ruta) {
        foreach ($l in Get-Content $ruta -Encoding UTF8) {
            $t = $l.Trim()
            if ($t -eq '' -or $t.StartsWith('#') -or -not $t.Contains('=')) { continue }
            $i = $t.IndexOf('=')
            $v[$t.Substring(0, $i).Trim()] = $t.Substring($i + 1).Trim().Trim('"', "'")
        }
    }
    return $v
}

function Clave-Aleatoria([int]$bytes = 24) {
    $b = New-Object byte[] $bytes
    [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b)
    # Solo letras y números para no romper la cadena de conexión
    return ([Convert]::ToBase64String($b) -replace '[^A-Za-z0-9]', '')
}

# Ejecuta SQL con psql dentro de un contenedor efímero (la contraseña viaja por variable de entorno, no por la línea de comandos)
function Ejecutar-Psql([string]$usuario, [string]$password, [string]$bd, [string]$sql, [string]$archivo) {
    $env:PGPASSWORD = $password
    try {
        $conn = "host=$Servidor port=5432 dbname=$bd user=$usuario sslmode=require"
        if ($archivo) {
            $dir = Split-Path $archivo -Parent
            $nombre = Split-Path $archivo -Leaf
            docker run --rm -e PGPASSWORD -v "${dir}:/sql:ro" $imagen psql $conn -v ON_ERROR_STOP=1 -q -f "/sql/$nombre"
        } else {
            docker run --rm -e PGPASSWORD $imagen psql $conn -v ON_ERROR_STOP=1 -q -c $sql
        }
        if ($LASTEXITCODE -ne 0) { throw "psql terminó con código $LASTEXITCODE" }
    } finally {
        Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
    }
}

$envLocal = Leer-Env (Join-Path $raiz '.env')
$salida = Join-Path $raiz 'azure-appsettings.json'
$previo = @{}
if (Test-Path $salida) {
    foreach ($x in (Get-Content $salida -Raw -Encoding UTF8 | ConvertFrom-Json)) { $previo[$x.name] = $x.value }
}

if ($SoloVariables) {
    if (-not $previo['ConnectionStrings__Default']) { throw 'No existe azure-appsettings.json previo con la cadena de conexión. Ejecuta el script sin -SoloVariables.' }
    $conexion = $previo['ConnectionStrings__Default']
} else {
    Write-Host "Servidor: $Servidor  |  Admin: $Admin  |  Base: $BaseDatos" -ForegroundColor Cyan
    $seguro = Read-Host "Contraseña del administrador '$Admin'" -AsSecureString
    $adminPass = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($seguro))

    Write-Host '1/4 Probando conexión...' -ForegroundColor Cyan
    Ejecutar-Psql $Admin $adminPass 'postgres' 'SELECT 1;' $null

    Write-Host "2/4 Creando usuario '$UsuarioApp' y base '$BaseDatos'..." -ForegroundColor Cyan
    $appPass = Clave-Aleatoria
    $sqlRol = @"
DO `$`$ BEGIN
  IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = '$UsuarioApp') THEN
    CREATE ROLE $UsuarioApp LOGIN PASSWORD '$appPass';
  ELSE
    ALTER ROLE $UsuarioApp WITH LOGIN PASSWORD '$appPass';
  END IF;
END `$`$;
GRANT $UsuarioApp TO $Admin;
"@
    Ejecutar-Psql $Admin $adminPass 'postgres' $sqlRol $null

    $env:PGPASSWORD = $adminPass
    try {
        $existe = docker run --rm -e PGPASSWORD $imagen psql "host=$Servidor dbname=postgres user=$Admin sslmode=require" -tAc "SELECT 1 FROM pg_database WHERE datname='$BaseDatos'"
    } finally { Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue }
    if ("$existe".Trim() -ne '1') {
        Ejecutar-Psql $Admin $adminPass 'postgres' "CREATE DATABASE $BaseDatos OWNER $UsuarioApp ENCODING 'UTF8';" $null
        $nueva = $true
    } else {
        Write-Host "   La base '$BaseDatos' ya existe; solo se actualiza la contraseña del usuario." -ForegroundColor Yellow
        $nueva = $false
    }
    # Extensiones (requieren admin) y permisos mínimos
    Ejecutar-Psql $Admin $adminPass $BaseDatos "CREATE EXTENSION IF NOT EXISTS pgcrypto; CREATE EXTENSION IF NOT EXISTS unaccent; REVOKE ALL ON DATABASE $BaseDatos FROM PUBLIC; GRANT CONNECT, TEMP ON DATABASE $BaseDatos TO $UsuarioApp; ALTER SCHEMA public OWNER TO $UsuarioApp;" $null

    if ($nueva) {
        Write-Host '3/4 Cargando esquema y datos iniciales...' -ForegroundColor Cyan
        Ejecutar-Psql $UsuarioApp $appPass $BaseDatos $null (Join-Path $raiz 'database\init\01_schema.sql')
        Ejecutar-Psql $UsuarioApp $appPass $BaseDatos $null (Join-Path $raiz 'database\init\02_seed.sql')
    } else {
        Write-Host '3/4 Aplicando migraciones (idempotentes)...' -ForegroundColor Cyan
        foreach ($m in Get-ChildItem (Join-Path $raiz 'database\migraciones') -Filter *.sql | Sort-Object Name) {
            Ejecutar-Psql $UsuarioApp $appPass $BaseDatos $null $m.FullName
        }
    }
    $adminPass = $null
    $conexion = "Host=$Servidor;Port=5432;Database=$BaseDatos;Username=$UsuarioApp;Password=$appPass;SSL Mode=Require;Trust Server Certificate=false"
}

Write-Host '4/4 Generando azure-appsettings.json...' -ForegroundColor Cyan
function V([string]$clave, [string]$porDefecto = '') { if ($envLocal[$clave]) { $envLocal[$clave] } else { $porDefecto } }
# En producción se generan claves NUEVAS (no reutilizar las de desarrollo); se conservan si ya existían
$jwt = if ($previo['Jwt__Key']) { $previo['Jwt__Key'] } else { Clave-Aleatoria 48 }
$pepper = if ($previo['Seguridad__PepperCodigos']) { $previo['Seguridad__PepperCodigos'] } else { Clave-Aleatoria 48 }

$vars = [ordered]@{
    'ASPNETCORE_ENVIRONMENT'      = 'Production'
    'ConnectionStrings__Default'  = $conexion
    'Jwt__Key'                    = $jwt
    'Seguridad__PepperCodigos'    = $pepper
    'OpenWeather__ApiKey'         = V 'OPENWEATHER_API_KEY'
    'YouTube__ApiKey'             = V 'YOUTUBE_API_KEY'
    'Smtp__Host'                  = V 'SMTP_HOST'
    'Smtp__Port'                  = V 'SMTP_PORT' '465'
    'Smtp__User'                  = V 'SMTP_USER'
    'Smtp__Password'              = V 'SMTP_PASSWORD'
    'Smtp__From'                  = V 'SMTP_FROM'
    'Smtp__FromName'              = 'Plantamoss'
    'Smtp__EnableSsl'             = V 'SMTP_ENABLE_SSL' 'true'
}
$lista = foreach ($k in $vars.Keys) { [pscustomobject]@{ name = $k; value = "$($vars[$k])"; slotSetting = $false } }
$lista | ConvertTo-Json -Depth 3 | Set-Content -Path $salida -Encoding UTF8

Write-Host ''
Write-Host "Listo. Variables en: $salida" -ForegroundColor Green
Write-Host 'Siguiente paso: Azure Portal > App Service > Variables de entorno > Edición avanzada,'
Write-Host 'pega el contenido de ese archivo (combinándolo con lo que ya haya) y guarda.'
Write-Host 'IMPORTANTE: ese archivo contiene secretos; no lo compartas ni lo subas a git.' -ForegroundColor Yellow
