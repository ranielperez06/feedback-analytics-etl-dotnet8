-- Execute this script while connected to the feedback_analytics database.
-- Create the etl_user role separately with the minimum required privileges.

CREATE SCHEMA IF NOT EXISTS source;
CREATE SCHEMA IF NOT EXISTS staging;
CREATE SCHEMA IF NOT EXISTS analytics;

CREATE TABLE IF NOT EXISTS source.reviews (
    review_id TEXT PRIMARY KEY,
    author_name TEXT NOT NULL,
    review_text TEXT NOT NULL,
    score NUMERIC(5, 2) NOT NULL CHECK (score BETWEEN 1 AND 5),
    created_at TIMESTAMPTZ NOT NULL,
    product_name TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS staging.extracted_feedback (
    staging_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    batch_id TEXT NOT NULL,
    record_id TEXT NOT NULL,
    source_type TEXT NOT NULL,
    source_name TEXT NOT NULL,
    external_id TEXT NOT NULL,
    author_name TEXT NOT NULL,
    content TEXT NOT NULL,
    score NUMERIC(5, 2) NULL,
    source_created_at TIMESTAMPTZ NOT NULL,
    extracted_at TIMESTAMPTZ NOT NULL,
    metadata JSONB NOT NULL DEFAULT '{}'::jsonb
);

CREATE INDEX IF NOT EXISTS ix_extracted_feedback_batch
    ON staging.extracted_feedback (batch_id);

CREATE INDEX IF NOT EXISTS ix_extracted_feedback_source
    ON staging.extracted_feedback (source_name, extracted_at);

CREATE TABLE IF NOT EXISTS analytics.feedback_fact (
    feedback_key BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    source_name TEXT NOT NULL,
    external_id TEXT NOT NULL,
    content TEXT NOT NULL,
    score NUMERIC(5, 2) NULL,
    event_date DATE NOT NULL,
    loaded_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (source_name, external_id)
);
