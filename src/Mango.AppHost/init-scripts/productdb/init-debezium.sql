-- Enable logical replication for Debezium CDC.
--
-- This runs via /docker-entrypoint-initdb.d, which executes ONCE, during the
-- very first initialization of an empty data directory, against the default
-- 'postgres' maintenance database. Nothing the application creates later
-- (databases, tables) exists yet at this point.
--
-- wal_level must be 'logical' for the pgoutput plugin. It cannot be changed
-- with SQL and postgresql.conf lives inside the data volume, so it is passed
-- as a server argument from Mango.AppHost (see AppHost.cs).

-- ---------------------------------------------------------------
--  Cluster-level roles
-- ---------------------------------------------------------------
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

-- ---------------------------------------------------------------
--  productdb
--
--  Created here because the grants and publication below are database-scoped
--  and would otherwise fail (or land in the wrong database). Aspire's
--  AddDatabase("productdb") tolerates the database already existing
--  (PostgreSQL error 42P04) and EF Core migrations only add tables to it, so
--  pre-creating it is safe.
-- ---------------------------------------------------------------
SELECT 'CREATE DATABASE productdb'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'productdb')\gexec

GRANT CONNECT ON DATABASE productdb TO debezium_user;

-- Everything below is database-scoped and must run inside productdb, not in
-- the default 'postgres' maintenance database.
\connect productdb

GRANT USAGE ON SCHEMA public TO debezium_user;

-- Grant SELECT on all tables in public schema (for CDC)
GRANT SELECT ON ALL TABLES IN SCHEMA public TO debezium_user;

-- The products table is created later by EF Core migrations, so also grant
-- SELECT on tables postgres creates from now on.
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

-- Create publication for Debezium (required for logical replication).
-- Debezium connects to productdb, so the publication must live there.
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_publication WHERE pubname = 'debezium_publication') THEN
        CREATE PUBLICATION debezium_publication FOR ALL TABLES;
    END IF;
END
$$;
