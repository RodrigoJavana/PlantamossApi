-- Ciudades fijadas por usuario en el widget de clima de Inicio.
-- Para bases ya creadas (las nuevas lo traen en init/01_schema.sql):
--   docker exec -i plantamoss-db psql -U plantamoss -d plantamoss < database/migraciones/2026-10-08_ciudades_clima.sql
ALTER TABLE users ADD COLUMN IF NOT EXISTS ciudades_clima text[] NULL;
