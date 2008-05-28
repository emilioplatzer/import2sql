CREATE ROLE import2sql LOGIN
  ENCRYPTED PASSWORD 'md5ed22fdc74436e11b55e3b96292a4df8b'
  NOSUPERUSER NOINHERIT NOCREATEDB NOCREATEROLE;
CREATE DATABASE "import2sqlDB"
  WITH OWNER = import2sql
       ENCODING = 'LATIN1'
       TABLESPACE = pg_default;
CREATE TABLE TablaExistente
(
   texto character varying(100),
   numero integer
) WITHOUT OIDS;
ALTER TABLE TablaExistente OWNER TO import2sql;
INSERT INTO tablaexistente (texto, numero) VALUES ('uno',1);

-- Function: primerpalabra(frase text)

-- DROP FUNCTION primerpalabra(frase text);

CREATE OR REPLACE FUNCTION primerpalabra(frase text)
  RETURNS text AS
$BODY$
begin
  return translate(substring(trim(both frase) from '^([^ .]+)(?:[ .]+|$)'),' .','');
end
$BODY$
  LANGUAGE 'plpgsql' IMMUTABLE STRICT;
ALTER FUNCTION primerpalabra(frase text) OWNER TO postgres;

-- Function: sinprimerpalabra(frase text)

-- DROP FUNCTION sinprimerpalabra(frase text);

CREATE OR REPLACE FUNCTION sinprimerpalabra(frase text)
  RETURNS text AS
$BODY$
begin
  return trim(both ' ' from trim(both '.' from regexp_replace(frase,'^[ .]*[^ .]+(?:[ .]|$)','')));
end
$BODY$
  LANGUAGE 'plpgsql' IMMUTABLE STRICT;
ALTER FUNCTION sinprimerpalabra(frase text) OWNER TO postgres;

CREATE OR REPLACE FUNCTION normalizar(frase text)
  RETURNS text AS
$BODY$
begin
  return translate(frase,'·ÈÌÛ˙‡ËÏÚ˘‰ÎÔˆ¸‚ÍÓÙ˚„ıÒ ¡…Õ”⁄¿»Ã“ŸƒÀœ÷‹¬ Œ‘€√’—',
                         'aeiouaeiouaeiouaeiouaon AEIOUAEIOUAEIOUAEIOUAON');
end
$BODY$
  LANGUAGE 'plpgsql' IMMUTABLE STRICT;
ALTER FUNCTION sinprimerpalabra(frase text) OWNER TO postgres;