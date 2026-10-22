-- =====================================================================
-- Plantamoss 2026 - Migración de INSIGNIAS (logros). Ver CONTRATO.md, sección 7.
-- Idempotente: se puede ejecutar varias veces sin error.
-- Para bases ya creadas (las nuevas lo traen en init/01_schema.sql):
--   docker exec -i plantamoss-db psql -U plantamoss -d plantamoss < database/migraciones/2026-10-21_insignias.sql
-- =====================================================================
SET client_encoding = 'UTF8';

BEGIN;

-- ---------------------------------------------------------------------
-- eventos_usuario: eventos que cuentan para las insignias (VER_PLANTA, VER_ESTADO, LEER_BLOG)
-- Una referencia (id de planta, estado o blog) cuenta una sola vez por usuario y evento.
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS eventos_usuario (
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
CREATE TABLE IF NOT EXISTS insignias_usuario (
    user_id            bigint       NOT NULL REFERENCES users (id) ON DELETE CASCADE,
    codigo             varchar(40)  NOT NULL,
    desbloqueada_en    timestamptz  NOT NULL DEFAULT now(),
    PRIMARY KEY (user_id, codigo)
);

COMMIT;
