-- Continuación de la Actividad 1: carga dimensional del Data Warehouse.
-- El script es idempotente: puede ejecutarse varias veces sin duplicar registros.
-- Debe ejecutarse después de cargar staging.extracted_feedback.

BEGIN;

CREATE SCHEMA IF NOT EXISTS analytics;

CREATE TABLE IF NOT EXISTS analytics.dim_date (
    date_key INTEGER PRIMARY KEY,
    full_date DATE NOT NULL UNIQUE,
    day_number SMALLINT NOT NULL,
    month_number SMALLINT NOT NULL,
    month_name TEXT NOT NULL,
    quarter_number SMALLINT NOT NULL,
    year_number SMALLINT NOT NULL,
    day_of_week_number SMALLINT NOT NULL,
    day_name TEXT NOT NULL,
    is_weekend BOOLEAN NOT NULL
);

CREATE TABLE IF NOT EXISTS analytics.dim_source (
    source_key BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    source_name TEXT NOT NULL UNIQUE,
    source_type TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS analytics.dim_author (
    author_key BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    author_name TEXT NOT NULL UNIQUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS analytics.dim_product (
    product_key BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    product_name TEXT NOT NULL UNIQUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

ALTER TABLE analytics.feedback_fact
    ADD COLUMN IF NOT EXISTS date_key INTEGER,
    ADD COLUMN IF NOT EXISTS source_key BIGINT,
    ADD COLUMN IF NOT EXISTS author_key BIGINT,
    ADD COLUMN IF NOT EXISTS product_key BIGINT;

CREATE UNIQUE INDEX IF NOT EXISTS ux_feedback_fact_source_external
    ON analytics.feedback_fact (source_name, external_id);

INSERT INTO analytics.dim_date (
    date_key, full_date, day_number, month_number, month_name,
    quarter_number, year_number, day_of_week_number, day_name, is_weekend)
SELECT DISTINCT
    TO_CHAR(source_created_at AT TIME ZONE 'UTC', 'YYYYMMDD')::INTEGER,
    (source_created_at AT TIME ZONE 'UTC')::DATE,
    EXTRACT(DAY FROM source_created_at AT TIME ZONE 'UTC')::SMALLINT,
    EXTRACT(MONTH FROM source_created_at AT TIME ZONE 'UTC')::SMALLINT,
    TRIM(TO_CHAR(source_created_at AT TIME ZONE 'UTC', 'Month')),
    EXTRACT(QUARTER FROM source_created_at AT TIME ZONE 'UTC')::SMALLINT,
    EXTRACT(YEAR FROM source_created_at AT TIME ZONE 'UTC')::SMALLINT,
    EXTRACT(ISODOW FROM source_created_at AT TIME ZONE 'UTC')::SMALLINT,
    TRIM(TO_CHAR(source_created_at AT TIME ZONE 'UTC', 'Day')),
    EXTRACT(ISODOW FROM source_created_at AT TIME ZONE 'UTC') IN (6, 7)
FROM staging.extracted_feedback
ON CONFLICT (date_key) DO UPDATE SET
    full_date = EXCLUDED.full_date,
    day_number = EXCLUDED.day_number,
    month_number = EXCLUDED.month_number,
    month_name = EXCLUDED.month_name,
    quarter_number = EXCLUDED.quarter_number,
    year_number = EXCLUDED.year_number,
    day_of_week_number = EXCLUDED.day_of_week_number,
    day_name = EXCLUDED.day_name,
    is_weekend = EXCLUDED.is_weekend;

INSERT INTO analytics.dim_source (source_name, source_type)
SELECT DISTINCT source_name, source_type
FROM staging.extracted_feedback
ON CONFLICT (source_name) DO UPDATE SET source_type = EXCLUDED.source_type;

INSERT INTO analytics.dim_author (author_name)
SELECT DISTINCT COALESCE(NULLIF(TRIM(author_name), ''), 'Autor desconocido')
FROM staging.extracted_feedback
ON CONFLICT (author_name) DO UPDATE SET updated_at = NOW();

INSERT INTO analytics.dim_product (product_name)
SELECT DISTINCT
    COALESCE(
        NULLIF(TRIM(metadata ->> 'product'), ''),
        NULLIF(TRIM(metadata ->> 'area'), ''),
        CASE
            WHEN NULLIF(TRIM(metadata ->> 'postId'), '') IS NOT NULL
                THEN 'Publicación ' || TRIM(metadata ->> 'postId')
        END,
        'No especificado')
FROM staging.extracted_feedback
ON CONFLICT (product_name) DO UPDATE SET updated_at = NOW();

WITH latest_staging AS (
    SELECT DISTINCT ON (source_name, external_id)
        source_name, external_id, author_name, content, score,
        source_created_at, metadata
    FROM staging.extracted_feedback
    ORDER BY source_name, external_id, extracted_at DESC, staging_id DESC
),
resolved AS (
    SELECT
        latest.source_name,
        latest.external_id,
        latest.content,
        latest.score,
        (latest.source_created_at AT TIME ZONE 'UTC')::DATE AS event_date,
        date_dim.date_key,
        source_dim.source_key,
        author_dim.author_key,
        product_dim.product_key
    FROM latest_staging AS latest
    JOIN analytics.dim_date AS date_dim
        ON date_dim.full_date = (latest.source_created_at AT TIME ZONE 'UTC')::DATE
    JOIN analytics.dim_source AS source_dim
        ON source_dim.source_name = latest.source_name
    JOIN analytics.dim_author AS author_dim
        ON author_dim.author_name =
           COALESCE(NULLIF(TRIM(latest.author_name), ''), 'Autor desconocido')
    JOIN analytics.dim_product AS product_dim
        ON product_dim.product_name =
           COALESCE(
               NULLIF(TRIM(latest.metadata ->> 'product'), ''),
               NULLIF(TRIM(latest.metadata ->> 'area'), ''),
               CASE
                   WHEN NULLIF(TRIM(latest.metadata ->> 'postId'), '') IS NOT NULL
                       THEN 'Publicación ' || TRIM(latest.metadata ->> 'postId')
               END,
               'No especificado')
)
INSERT INTO analytics.feedback_fact (
    source_name, external_id, content, score, event_date,
    date_key, source_key, author_key, product_key, loaded_at)
SELECT
    source_name, external_id, content, score, event_date,
    date_key, source_key, author_key, product_key, NOW()
FROM resolved
ON CONFLICT (source_name, external_id) DO UPDATE SET
    content = EXCLUDED.content,
    score = EXCLUDED.score,
    event_date = EXCLUDED.event_date,
    date_key = EXCLUDED.date_key,
    source_key = EXCLUDED.source_key,
    author_key = EXCLUDED.author_key,
    product_key = EXCLUDED.product_key,
    loaded_at = NOW();

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'fk_feedback_fact_date'
          AND conrelid = 'analytics.feedback_fact'::regclass) THEN
        ALTER TABLE analytics.feedback_fact
            ADD CONSTRAINT fk_feedback_fact_date
            FOREIGN KEY (date_key) REFERENCES analytics.dim_date(date_key) NOT VALID;
    END IF;
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'fk_feedback_fact_source'
          AND conrelid = 'analytics.feedback_fact'::regclass) THEN
        ALTER TABLE analytics.feedback_fact
            ADD CONSTRAINT fk_feedback_fact_source
            FOREIGN KEY (source_key) REFERENCES analytics.dim_source(source_key) NOT VALID;
    END IF;
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'fk_feedback_fact_author'
          AND conrelid = 'analytics.feedback_fact'::regclass) THEN
        ALTER TABLE analytics.feedback_fact
            ADD CONSTRAINT fk_feedback_fact_author
            FOREIGN KEY (author_key) REFERENCES analytics.dim_author(author_key) NOT VALID;
    END IF;
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'fk_feedback_fact_product'
          AND conrelid = 'analytics.feedback_fact'::regclass) THEN
        ALTER TABLE analytics.feedback_fact
            ADD CONSTRAINT fk_feedback_fact_product
            FOREIGN KEY (product_key) REFERENCES analytics.dim_product(product_key) NOT VALID;
    END IF;
END
$$;

ALTER TABLE analytics.feedback_fact VALIDATE CONSTRAINT fk_feedback_fact_date;
ALTER TABLE analytics.feedback_fact VALIDATE CONSTRAINT fk_feedback_fact_source;
ALTER TABLE analytics.feedback_fact VALIDATE CONSTRAINT fk_feedback_fact_author;
ALTER TABLE analytics.feedback_fact VALIDATE CONSTRAINT fk_feedback_fact_product;

COMMIT;
