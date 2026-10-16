-- =====================================================================
-- Plantamoss 2026 - Migración de SEGURIDAD (Unidad II) - CONTRATO 5.1 y 5.2
-- Idempotente: se puede ejecutar varias veces sin error.
-- Para bases ya creadas (las nuevas lo traen en init/01_schema.sql):
--   docker exec -i plantamoss-db psql -U plantamoss -d plantamoss < database/migraciones/2026-10-08_seguridad.sql
-- =====================================================================
SET client_encoding = 'UTF8';

BEGIN;

-- ---------------------------------------------------------------------
-- 1) password_reset_codes.code: ahora guarda el HMAC-SHA256 (64 hex), no el código en claro.
--    Los códigos pendientes en claro dejan de servir: se borran (duran 15 min; el usuario pide otro).
-- ---------------------------------------------------------------------
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'password_reset_codes' AND column_name = 'code'
          AND character_maximum_length IS DISTINCT FROM 64
    ) THEN
        DELETE FROM password_reset_codes;
        ALTER TABLE password_reset_codes ALTER COLUMN code TYPE varchar(64);
    END IF;
END $$;

COMMENT ON COLUMN password_reset_codes.code IS 'HMAC-SHA256(pepper, tipo:email:codigo) en hex; nunca el código en claro';

-- ---------------------------------------------------------------------
-- 2) refresh_tokens (CONTRATO 5.1): solo se guarda el SHA-256 del token
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS refresh_tokens (
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

CREATE INDEX IF NOT EXISTS ix_refresh_tokens_user_id ON refresh_tokens (user_id);
CREATE INDEX IF NOT EXISTS ix_refresh_tokens_expira_en ON refresh_tokens (expira_en);

-- ---------------------------------------------------------------------
-- 3) bitacora_auditoria (CONTRATO 5.2)
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS bitacora_auditoria (
    id                 bigserial    PRIMARY KEY,
    fecha              timestamptz  NOT NULL DEFAULT now(),
    user_id            bigint       NULL REFERENCES users (id) ON DELETE SET NULL,
    email              varchar(255) NULL,
    accion             varchar(60)  NOT NULL,
    detalle            varchar(500) NULL,
    ip                 varchar(45)  NULL,
    exito              boolean      NOT NULL DEFAULT true
);

CREATE INDEX IF NOT EXISTS ix_bitacora_fecha  ON bitacora_auditoria (fecha DESC);
CREATE INDEX IF NOT EXISTS ix_bitacora_accion ON bitacora_auditoria (accion);
CREATE INDEX IF NOT EXISTS ix_bitacora_email  ON bitacora_auditoria (lower(email));
CREATE INDEX IF NOT EXISTS ix_bitacora_user   ON bitacora_auditoria (user_id);

COMMIT;
