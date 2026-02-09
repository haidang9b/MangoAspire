-- Enable logical replication for Debezium CDC
-- This script runs when PostgreSQL container starts

-- The WAL level must be set in postgresql.conf, not via SQL
-- Aspire's PostgreSQL container uses wal_level=logical by default when using pgoutput

-- Create replication role for Debezium
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'debezium_user') THEN
        CREATE ROLE debezium_user WITH REPLICATION LOGIN PASSWORD 'debezium';
    END IF;
END
$$;

-- Create replication group for shared ownership
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'replication_group') THEN
        CREATE ROLE replication_group;
    END IF;
END
$$;

-- Grant replication_group to postgres (original owner)
GRANT replication_group TO postgres;

-- Grant replication_group to debezium_user
GRANT replication_group TO debezium_user;

-- Grant necessary permissions on the productdb database
GRANT CONNECT ON DATABASE productdb TO debezium_user;
GRANT USAGE ON SCHEMA public TO debezium_user;

-- Grant SELECT on all tables in public schema (for CDC)
GRANT SELECT ON ALL TABLES IN SCHEMA public TO debezium_user;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT ON TABLES TO debezium_user;

-- Change ownership of products table to replication_group
-- This allows both postgres and debezium_user to access it
DO $$
BEGIN
    IF EXISTS (SELECT FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'products') THEN
        ALTER TABLE public.products OWNER TO replication_group;
    END IF;
END
$$;

-- Create publication for Debezium (required for logical replication)
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_publication WHERE pubname = 'debezium_publication') THEN
        CREATE PUBLICATION debezium_publication FOR ALL TABLES;
    END IF;
END
$$;
