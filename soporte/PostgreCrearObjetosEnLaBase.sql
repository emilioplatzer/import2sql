CREATE ROLE import2sql LOGIN
  ENCRYPTED PASSWORD 'md5ed22fdc74436e11b55e3b96292a4df8b'
  NOSUPERUSER NOINHERIT NOCREATEDB NOCREATEROLE;
CREATE DATABASE import2sqlDB
  WITH OWNER = import2sql
       ENCODING = 'LATIN1'
       TABLESPACE = pg_default;
CREATE TABLE "TablaExistente"
(
   texto character varying(100),
   numero integer
) WITHOUT OIDS;
ALTER TABLE "TablaExistente" OWNER TO import2sql;
