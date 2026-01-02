-- Script para limpar completamente o banco de dados do Keycloak
-- ATENÇÃO: Isso vai deletar TODOS os dados do Keycloak!

-- Desabilita temporariamente as constraints de foreign key
SET session_replication_role = 'replica';

-- Deleta todas as tabelas do Keycloak (em ordem para evitar problemas de foreign key)
DROP TABLE IF EXISTS credential CASCADE;
DROP TABLE IF EXISTS user_attribute CASCADE;
DROP TABLE IF EXISTS user_entity CASCADE;
DROP TABLE IF EXISTS user_role_mapping CASCADE;
DROP TABLE IF EXISTS user_group_membership CASCADE;
DROP TABLE IF EXISTS federated_identity CASCADE;
DROP TABLE IF EXISTS user_consent CASCADE;
DROP TABLE IF EXISTS user_required_actions CASCADE;
DROP TABLE IF EXISTS databasechangelog CASCADE;
DROP TABLE IF EXISTS databasechangeloglock CASCADE;

-- Deleta outras tabelas comuns do Keycloak
DROP TABLE IF EXISTS realm CASCADE;
DROP TABLE IF EXISTS realm_attribute CASCADE;
DROP TABLE IF EXISTS client CASCADE;
DROP TABLE IF EXISTS client_attributes CASCADE;
DROP TABLE IF EXISTS client_scope CASCADE;
DROP TABLE IF EXISTS client_scope_attributes CASCADE;
DROP TABLE IF EXISTS role CASCADE;
DROP TABLE IF EXISTS keycloak_role CASCADE;
DROP TABLE IF EXISTS composite_role CASCADE;
DROP TABLE IF EXISTS realm_roles CASCADE;
DROP TABLE IF EXISTS client_roles CASCADE;
DROP TABLE IF EXISTS group_role_mapping CASCADE;
DROP TABLE IF EXISTS user_group_membership CASCADE;
DROP TABLE IF EXISTS user_role_mapping CASCADE;
DROP TABLE IF EXISTS identity_provider CASCADE;
DROP TABLE IF EXISTS identity_provider_mapper CASCADE;
DROP TABLE IF EXISTS identity_provider_config CASCADE;
DROP TABLE IF EXISTS protocol_mapper CASCADE;
DROP TABLE IF EXISTS protocol_mapper_config CASCADE;
DROP TABLE IF EXISTS component CASCADE;
DROP TABLE IF EXISTS component_config CASCADE;
DROP TABLE IF EXISTS authentication_execution CASCADE;
DROP TABLE IF EXISTS authentication_flow CASCADE;
DROP TABLE IF EXISTS authentication_requirement CASCADE;
DROP TABLE IF EXISTS authentication_config CASCADE;
DROP TABLE IF EXISTS required_action_provider CASCADE;
DROP TABLE IF EXISTS required_action_config CASCADE;
DROP TABLE IF EXISTS event_entity CASCADE;
DROP TABLE IF EXISTS admin_event_entity CASCADE;
DROP TABLE IF EXISTS client_session CASCADE;
DROP TABLE IF EXISTS client_session_auth_status CASCADE;
DROP TABLE IF EXISTS client_session_note CASCADE;
DROP TABLE IF EXISTS client_session_prot_mapper CASCADE;
DROP TABLE IF EXISTS client_session_role CASCADE;
DROP TABLE IF EXISTS user_session CASCADE;
DROP TABLE IF EXISTS user_session_note CASCADE;
DROP TABLE IF EXISTS offline_user_session CASCADE;
DROP TABLE IF EXISTS offline_client_session CASCADE;
DROP TABLE IF EXISTS login_failure CASCADE;
DROP TABLE IF EXISTS migration_model CASCADE;

-- Tenta deletar todas as outras tabelas que começam com padrões comuns do Keycloak
DO $$
DECLARE
    r RECORD;
BEGIN
    FOR r IN (SELECT tablename FROM pg_tables WHERE schemaname = 'public' AND tablename LIKE '%keycloak%' OR tablename LIKE '%realm%' OR tablename LIKE '%client%' OR tablename LIKE '%user%' OR tablename LIKE '%credential%' OR tablename LIKE '%role%' OR tablename LIKE '%session%' OR tablename LIKE '%event%' OR tablename LIKE '%migration%' OR tablename LIKE '%databasechangelog%') 
    LOOP
        EXECUTE 'DROP TABLE IF EXISTS ' || quote_ident(r.tablename) || ' CASCADE';
    END LOOP;
END $$;

-- Reabilita as constraints
SET session_replication_role = 'origin';

SELECT 'Banco de dados do Keycloak limpo! Reinicie o Keycloak para recriar tudo do zero.' as status;





