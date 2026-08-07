-- Run as a PostgreSQL administrator after creating the etl_user role.
-- Example role creation (replace the password before executing):
-- CREATE ROLE etl_user WITH LOGIN PASSWORD 'use-a-strong-secret';

GRANT CONNECT ON DATABASE feedback_analytics TO etl_user;
GRANT USAGE ON SCHEMA source, staging, analytics TO etl_user;
GRANT SELECT ON ALL TABLES IN SCHEMA source TO etl_user;
GRANT SELECT, INSERT ON ALL TABLES IN SCHEMA staging TO etl_user;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA staging TO etl_user;
GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA analytics TO etl_user;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA analytics TO etl_user;

ALTER DEFAULT PRIVILEGES IN SCHEMA source
    GRANT SELECT ON TABLES TO etl_user;

ALTER DEFAULT PRIVILEGES IN SCHEMA staging
    GRANT SELECT, INSERT ON TABLES TO etl_user;

ALTER DEFAULT PRIVILEGES IN SCHEMA analytics
    GRANT SELECT, INSERT, UPDATE ON TABLES TO etl_user;

ALTER DEFAULT PRIVILEGES IN SCHEMA analytics
    GRANT USAGE, SELECT ON SEQUENCES TO etl_user;
