INSERT INTO source.reviews
    (review_id, author_name, review_text, score, created_at, product_name)
VALUES
    ('REV-001', 'Pedro Ramírez', 'Excelente calidad y entrega puntual.', 5, '2026-07-27T13:30:00-04:00', 'Plan Premium'),
    ('REV-002', 'Sofía Herrera', 'El panel es útil pero puede cargar más rápido.', 4, '2026-07-28T08:40:00-04:00', 'Panel Analítico'),
    ('REV-003', 'Miguel Santos', 'Necesité ayuda para completar la configuración inicial.', 3, '2026-07-29T15:25:00-04:00', 'Integración API'),
    ('REV-004', 'Daniela Cruz', 'La documentación fue clara y completa.', 5, '2026-07-30T12:00:00-04:00', 'Integración API'),
    ('REV-005', 'Roberto Peña', 'Me gustaría exportar reportes en más formatos.', 4, '2026-08-01T17:35:00-04:00', 'Panel Analítico')
ON CONFLICT (review_id) DO UPDATE SET
    author_name = EXCLUDED.author_name,
    review_text = EXCLUDED.review_text,
    score = EXCLUDED.score,
    created_at = EXCLUDED.created_at,
    product_name = EXCLUDED.product_name;
