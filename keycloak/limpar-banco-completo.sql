-- Script para limpar COMPLETAMENTE o banco de dados do Keycloak
-- Deleta TODAS as tabelas do schema public

-- Primeiro, deleta todas as tabelas que o Keycloak usa
DO $$
DECLARE
    r RECORD;
    tables_to_drop TEXT[];
BEGIN
    -- Lista todas as tabelas do schema public
    SELECT array_agg(tablename) INTO tables_to_drop
    FROM pg_tables 
    WHERE schemaname = 'public';
    
    -- Deleta cada tabela
    IF tables_to_drop IS NOT NULL THEN
        FOREACH r.tablename IN ARRAY tables_to_drop
        LOOP
            BEGIN
                EXECUTE 'DROP TABLE IF EXISTS ' || quote_ident(r.tablename) || ' CASCADE';
            EXCEPTION WHEN OTHERS THEN
                RAISE NOTICE 'Erro ao deletar tabela %: %', r.tablename, SQLERRM;
            END;
        END LOOP;
    END IF;
END $$;

-- Verifica se ainda há tabelas
SELECT 'Tabelas restantes: ' || count(*)::text as status
FROM pg_tables 
WHERE schemaname = 'public';

SELECT 'Banco de dados limpo! Reinicie o Keycloak para recriar tudo do zero.' as resultado;



