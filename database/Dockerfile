# Plantamoss 2026 - PostgreSQL 16 (imagen propia)
FROM postgres:16-alpine

# Zona horaria del sistema (Monterrey) y datos tz para el contenedor
RUN apk add --no-cache tzdata

ENV TZ=America/Monterrey \
    PGTZ=America/Monterrey \
    LANG=C.UTF-8 \
    LC_ALL=C.UTF-8 \
    POSTGRES_DB=plantamoss \
    POSTGRES_USER=plantamoss \
    POSTGRES_PASSWORD=plantamoss_dev \
    POSTGRES_INITDB_ARGS="--encoding=UTF8 --locale=C.UTF-8"

# Scripts de inicialización (se ejecutan en orden alfabético solo si el volumen está vacío)
COPY init/*.sql /docker-entrypoint-initdb.d/

# Afinación ligera (incluida desde el postgresql.conf generado por initdb)
COPY conf/plantamoss.conf /etc/postgresql/plantamoss.conf
RUN echo "include '/etc/postgresql/plantamoss.conf'" >> /usr/local/share/postgresql/postgresql.conf.sample

EXPOSE 5432

HEALTHCHECK --interval=5s --timeout=5s --start-period=20s --retries=10 \
    CMD pg_isready -U "$POSTGRES_USER" -d "$POSTGRES_DB" -h 127.0.0.1 || exit 1
