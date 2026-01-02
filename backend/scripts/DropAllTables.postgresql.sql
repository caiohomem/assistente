-- Script para dropar todas as tabelas do banco de dados PostgreSQL
-- ATENÇÃO: Este script irá remover TODAS as tabelas e seus dados!
-- Use apenas em ambiente de desenvolvimento ou quando quiser recriar o banco do zero.
--
-- Execute este script antes de aplicar a migration inicial se quiser limpar o banco completamente.

DO $$ 
DECLARE
    r RECORD;
BEGIN
    -- Desabilitar verificação de chaves estrangeiras temporariamente
    SET session_replication_role = 'replica';
    
    -- Dropar todas as tabelas
    FOR r IN (SELECT tablename FROM pg_tables WHERE schemaname = 'public') 
    LOOP
        EXECUTE 'DROP TABLE IF EXISTS ' || quote_ident(r.tablename) || ' CASCADE';
        RAISE NOTICE 'Tabela % removida', r.tablename;
    END LOOP;
    
    -- Dropar todas as sequences (caso existam sequences órfãs)
    FOR r IN (SELECT sequence_name FROM information_schema.sequences WHERE sequence_schema = 'public')
    LOOP
        EXECUTE 'DROP SEQUENCE IF EXISTS ' || quote_ident(r.sequence_name) || ' CASCADE';
        RAISE NOTICE 'Sequence % removida', r.sequence_name;
    END LOOP; 
    
    -- Reabilitar verificação de chaves estrangeiras
    SET session_replication_role = 'origin';
    
    RAISE NOTICE 'Todas as tabelas e sequences foram removidas com sucesso!';
END $$;









