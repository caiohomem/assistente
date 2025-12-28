-- Script SIMPLES para dropar todas as tabelas do banco de dados PostgreSQL
-- ATENÇÃO: Este script irá remover TODAS as tabelas e seus dados!
-- Use apenas em ambiente de desenvolvimento ou quando quiser recriar o banco do zero.
--
-- Execute este script antes de aplicar a migration inicial se quiser limpar o banco completamente.

-- Dropar todas as tabelas em cascata (remove dependências automaticamente)
DROP SCHEMA public CASCADE;
CREATE SCHEMA public;

-- Restaurar permissões padrão
GRANT ALL ON SCHEMA public TO postgres;
GRANT ALL ON SCHEMA public TO public;





