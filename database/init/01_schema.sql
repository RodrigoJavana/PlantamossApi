-- =====================================================================
-- Plantamoss 2026 - Esquema PostgreSQL (ver CONTRATO.md, sección 1)
-- =====================================================================
SET client_encoding = 'UTF8';

CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS unaccent;

-- ---------------------------------------------------------------------
-- users
-- ---------------------------------------------------------------------
CREATE TABLE users (
    id                 bigserial    PRIMARY KEY,
    name               varchar(255) NOT NULL,
    email              varchar(255) NOT NULL UNIQUE,
    email_verified_at  timestamptz  NULL,
    password           varchar(255) NOT NULL,
    role               varchar(20)  NOT NULL DEFAULT 'user',
    estado             varchar(20)  NOT NULL DEFAULT 'activo',
    ciudades_clima     text[]       NULL,                   -- ciudades fijadas en el clima de Inicio (null = por defecto)
    created_at         timestamptz  NOT NULL DEFAULT now(),
    updated_at         timestamptz  NOT NULL DEFAULT now(),
    CONSTRAINT users_role_check   CHECK (role IN ('user', 'editor', 'noticias', 'admin')),
    CONSTRAINT users_estado_check CHECK (estado IN ('activo', 'suspendido'))
);

-- ---------------------------------------------------------------------
-- estados
-- ---------------------------------------------------------------------
CREATE TABLE estados (
    id_estado          serial       PRIMARY KEY,
    nombre_estado      varchar(100) NOT NULL UNIQUE,
    clima_predominante varchar(100) NULL
);

-- ---------------------------------------------------------------------
-- catalogo_flora
-- ---------------------------------------------------------------------
CREATE TABLE catalogo_flora (
    id_planta          serial       PRIMARY KEY,
    nombre_planta      varchar(255) NOT NULL,
    nombre_cientifico  varchar(255) NULL,
    letra_inicial      char(1)      NOT NULL,
    id_estado          int          NULL REFERENCES estados (id_estado) ON DELETE SET NULL,
    url_imagen         varchar(500) NULL,
    tipo_clima         varchar(50)  NOT NULL DEFAULT 'templado',
    luz_sol            varchar(50)  NOT NULL DEFAULT 'sol_directo',
    riego              varchar(100) NULL,
    descripcion        text         NULL,
    cuidados_texto     text         NULL,
    created_at         timestamptz  NOT NULL DEFAULT now(),
    updated_at         timestamptz  NOT NULL DEFAULT now(),
    CONSTRAINT catalogo_flora_tipo_clima_check CHECK (tipo_clima IN ('tropical', 'arido', 'templado', 'frio')),
    CONSTRAINT catalogo_flora_luz_sol_check    CHECK (luz_sol IN ('sol_directo', 'sombra_parcial', 'sombra'))
);

CREATE INDEX ix_catalogo_flora_id_estado     ON catalogo_flora (id_estado);
CREATE INDEX ix_catalogo_flora_nombre_lower  ON catalogo_flora (lower(nombre_planta));

-- ---------------------------------------------------------------------
-- noticias
-- ---------------------------------------------------------------------
CREATE TABLE noticias (
    id_noticia         serial       PRIMARY KEY,
    titulo             varchar(255) NOT NULL,
    contenido          text         NOT NULL,
    url_imagen         varchar(500) NULL,
    url_externa        varchar(500) NULL,
    destacada          boolean      NOT NULL DEFAULT false,
    activa             boolean      NOT NULL DEFAULT true,
    id_editor          bigint       NULL REFERENCES users (id) ON DELETE SET NULL,
    fecha_publicacion  timestamptz  NOT NULL DEFAULT now()
);

CREATE INDEX ix_noticias_activa_destacada_fecha
    ON noticias (activa, destacada, fecha_publicacion DESC);

-- ---------------------------------------------------------------------
-- miniblogs
-- ---------------------------------------------------------------------
CREATE TABLE miniblogs (
    id_blog            serial       PRIMARY KEY,
    id_usuario         bigint       NULL REFERENCES users (id) ON DELETE SET NULL,
    titulo             varchar(255) NOT NULL,
    contenido          text         NOT NULL,
    contenido2         text         NULL,
    contenido3         text         NULL,
    url_imagen         varchar(500) NULL,
    url_imagen_2       varchar(500) NULL,
    url_imagen_3       varchar(500) NULL,
    url_imagen_4       varchar(500) NULL,
    url_imagen_5       varchar(500) NULL,
    fecha_publicacion  timestamptz  NOT NULL DEFAULT now()
);

-- ---------------------------------------------------------------------
-- password_reset_codes (recuperar contraseña y verificar correo)
-- ---------------------------------------------------------------------
CREATE TABLE password_reset_codes (
    id                 bigserial    PRIMARY KEY,
    email              varchar(255) NOT NULL,
    code               varchar(64)  NOT NULL,              -- HMAC-SHA256 (hex) del código, nunca el código en claro
    tipo               varchar(20)  NOT NULL DEFAULT 'reset',
    intentos           int          NOT NULL DEFAULT 0,
    expires_at         timestamptz  NOT NULL,
    created_at         timestamptz  NOT NULL DEFAULT now(),
    CONSTRAINT password_reset_codes_tipo_check CHECK (tipo IN ('reset', 'verificacion'))
);

CREATE INDEX ix_password_reset_codes_email ON password_reset_codes (email);

-- ---------------------------------------------------------------------
-- refresh_tokens (CONTRATO 5.1): sesiones con rotación; solo se guarda el SHA-256 del token
-- ---------------------------------------------------------------------
CREATE TABLE refresh_tokens (
    id                 bigserial    PRIMARY KEY,
    user_id            bigint       NOT NULL REFERENCES users (id) ON DELETE CASCADE,
    token_hash         char(64)     NOT NULL UNIQUE,
    expira_en          timestamptz  NOT NULL,
    creado_en          timestamptz  NOT NULL DEFAULT now(),
    revocado_en        timestamptz  NULL,
    reemplazado_por    char(64)     NULL,
    ip                 varchar(45)  NULL,
    user_agent         varchar(300) NULL
);

CREATE INDEX ix_refresh_tokens_user_id   ON refresh_tokens (user_id);
CREATE INDEX ix_refresh_tokens_expira_en ON refresh_tokens (expira_en);

-- ---------------------------------------------------------------------
-- bitacora_auditoria (CONTRATO 5.2): quién hizo qué, cuándo y desde dónde
-- ---------------------------------------------------------------------
CREATE TABLE bitacora_auditoria (
    id                 bigserial    PRIMARY KEY,
    fecha              timestamptz  NOT NULL DEFAULT now(),
    user_id            bigint       NULL REFERENCES users (id) ON DELETE SET NULL,
    email              varchar(255) NULL,
    accion             varchar(60)  NOT NULL,
    detalle            varchar(500) NULL,
    ip                 varchar(45)  NULL,
    exito              boolean      NOT NULL DEFAULT true
);

CREATE INDEX ix_bitacora_fecha  ON bitacora_auditoria (fecha DESC);
CREATE INDEX ix_bitacora_accion ON bitacora_auditoria (accion);
CREATE INDEX ix_bitacora_email  ON bitacora_auditoria (lower(email));
CREATE INDEX ix_bitacora_user   ON bitacora_auditoria (user_id);

-- ---------------------------------------------------------------------
-- rachas: días seguidos de visita (día = fecha local de México) y planta que crece
-- ---------------------------------------------------------------------
CREATE TABLE rachas (
    user_id            bigint       PRIMARY KEY REFERENCES users (id) ON DELETE CASCADE,
    racha_actual       int          NOT NULL DEFAULT 0,
    racha_maxima       int          NOT NULL DEFAULT 0,
    total_dias         int          NOT NULL DEFAULT 0,
    ultimo_dia         date         NULL,
    actualizado_en     timestamptz  NOT NULL DEFAULT now()
);

-- ---------------------------------------------------------------------
-- eventos_usuario: eventos que cuentan para las insignias (VER_PLANTA, VER_ESTADO, LEER_BLOG)
-- Una referencia (id de planta, estado o blog) cuenta una sola vez por usuario y evento.
-- ---------------------------------------------------------------------
CREATE TABLE eventos_usuario (
    id                 bigserial    PRIMARY KEY,
    user_id            bigint       NOT NULL REFERENCES users (id) ON DELETE CASCADE,
    evento             varchar(30)  NOT NULL,
    referencia         varchar(64)  NOT NULL,
    creado_en          timestamptz  NOT NULL DEFAULT now(),
    CONSTRAINT eventos_usuario_unico UNIQUE (user_id, evento, referencia)
);

-- ---------------------------------------------------------------------
-- insignias_usuario: insignias desbloqueadas (cada una una sola vez por usuario)
-- ---------------------------------------------------------------------
CREATE TABLE insignias_usuario (
    user_id            bigint       NOT NULL REFERENCES users (id) ON DELETE CASCADE,
    codigo             varchar(40)  NOT NULL,
    desbloqueada_en    timestamptz  NOT NULL DEFAULT now(),
    PRIMARY KEY (user_id, codigo)
);

-- ---------------------------------------------------------------------
-- Triggers
-- ---------------------------------------------------------------------

-- updated_at automático
CREATE OR REPLACE FUNCTION fn_set_updated_at() RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
    NEW.updated_at := now();
    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_users_updated_at
    BEFORE UPDATE ON users
    FOR EACH ROW EXECUTE FUNCTION fn_set_updated_at();

CREATE TRIGGER trg_catalogo_flora_updated_at
    BEFORE UPDATE ON catalogo_flora
    FOR EACH ROW EXECUTE FUNCTION fn_set_updated_at();

-- Red de seguridad: la API calcula letra_inicial, pero si llega vacía
-- se toma la mayúscula de la primera letra de nombre_planta.
CREATE OR REPLACE FUNCTION fn_catalogo_flora_letra_inicial() RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
    IF NEW.letra_inicial IS NULL OR btrim(NEW.letra_inicial) = '' THEN
        NEW.letra_inicial := upper(left(btrim(NEW.nombre_planta), 1));
    END IF;
    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_catalogo_flora_letra_inicial
    BEFORE INSERT OR UPDATE ON catalogo_flora
    FOR EACH ROW EXECUTE FUNCTION fn_catalogo_flora_letra_inicial();
