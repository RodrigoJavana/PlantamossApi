-- =====================================================================
-- Plantamoss 2026 - Migración de RACHAS (días seguidos de visita y planta que crece)
-- Idempotente: se puede ejecutar varias veces sin error.
-- Para bases ya creadas (las nuevas lo traen en init/01_schema.sql):
--   docker exec -i plantamoss-db psql -U plantamoss -d plantamoss < database/migraciones/2026-10-21_rachas.sql
-- =====================================================================
SET client_encoding = 'UTF8';

BEGIN;

-- ---------------------------------------------------------------------
-- rachas: una fila por usuario. El "día" es la fecha local de México.
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS rachas (
    user_id            bigint       PRIMARY KEY REFERENCES users (id) ON DELETE CASCADE,
    racha_actual       int          NOT NULL DEFAULT 0,
    racha_maxima       int          NOT NULL DEFAULT 0,
    total_dias         int          NOT NULL DEFAULT 0,
    ultimo_dia         date         NULL,
    actualizado_en     timestamptz  NOT NULL DEFAULT now()
);

COMMIT;
