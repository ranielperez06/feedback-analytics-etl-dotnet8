-- Evidencia funcional: conteos, integridad y muestra del esquema estrella.

SELECT 'dim_date' AS table_name, COUNT(*) AS row_count
FROM analytics.dim_date
UNION ALL
SELECT 'dim_source', COUNT(*) FROM analytics.dim_source
UNION ALL
SELECT 'dim_author', COUNT(*) FROM analytics.dim_author
UNION ALL
SELECT 'dim_product', COUNT(*) FROM analytics.dim_product
UNION ALL
SELECT 'feedback_fact', COUNT(*) FROM analytics.feedback_fact
ORDER BY table_name;

SELECT
    fact.feedback_key,
    date_dim.full_date,
    source_dim.source_name,
    source_dim.source_type,
    author_dim.author_name,
    product_dim.product_name,
    fact.score,
    LEFT(fact.content, 80) AS feedback
FROM analytics.feedback_fact AS fact
JOIN analytics.dim_date AS date_dim
    ON date_dim.date_key = fact.date_key
JOIN analytics.dim_source AS source_dim
    ON source_dim.source_key = fact.source_key
JOIN analytics.dim_author AS author_dim
    ON author_dim.author_key = fact.author_key
JOIN analytics.dim_product AS product_dim
    ON product_dim.product_key = fact.product_key
ORDER BY date_dim.full_date, fact.feedback_key
LIMIT 25;

SELECT COUNT(*) AS orphan_facts
FROM analytics.feedback_fact AS fact
LEFT JOIN analytics.dim_date AS date_dim ON date_dim.date_key = fact.date_key
LEFT JOIN analytics.dim_source AS source_dim ON source_dim.source_key = fact.source_key
LEFT JOIN analytics.dim_author AS author_dim ON author_dim.author_key = fact.author_key
LEFT JOIN analytics.dim_product AS product_dim ON product_dim.product_key = fact.product_key
WHERE date_dim.date_key IS NULL
   OR source_dim.source_key IS NULL
   OR author_dim.author_key IS NULL
   OR product_dim.product_key IS NULL;

SELECT source_name, external_id, COUNT(*) AS duplicate_count
FROM analytics.feedback_fact
GROUP BY source_name, external_id
HAVING COUNT(*) > 1;
