-- Deleta o usuário admin completamente para que o Keycloak recrie com KEYCLOAK_ADMIN/KEYCLOAK_ADMIN_PASSWORD

-- Deletar credenciais primeiro
DELETE FROM credential WHERE user_id IN (SELECT id FROM user_entity WHERE username = 'admin');

-- Deletar user_role_mappings
DELETE FROM user_role_mapping WHERE user_id IN (SELECT id FROM user_entity WHERE username = 'admin');

-- Deletar user_group_membership
DELETE FROM user_group_membership WHERE user_id IN (SELECT id FROM user_entity WHERE username = 'admin');

-- Deletar user_attribute (pode ter nome diferente)
DELETE FROM user_attribute WHERE user_id IN (SELECT id FROM user_entity WHERE username = 'admin');

-- Deletar federated_identity
DELETE FROM federated_identity WHERE user_id IN (SELECT id FROM user_entity WHERE username = 'admin');

-- Deletar user_consent
DELETE FROM user_consent WHERE user_id IN (SELECT id FROM user_entity WHERE username = 'admin');

-- Deletar o usuário admin
DELETE FROM user_entity WHERE username = 'admin';

-- Verificar se foi deletado
SELECT 'Usuário admin deletado. Reinicie o Keycloak para recriar com KEYCLOAK_ADMIN/KEYCLOAK_ADMIN_PASSWORD' as status;

