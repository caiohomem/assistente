-- Script simples para resetar o admin do Keycloak
-- Deleta as credenciais do admin para que o Keycloak recrie com KEYCLOAK_ADMIN/KEYCLOAK_ADMIN_PASSWORD

-- Deletar credenciais do usuário admin
DELETE FROM credential 
WHERE user_id IN (SELECT id FROM user_entity WHERE username = 'admin');

-- Verificar se foi deletado
SELECT 'Credenciais do admin deletadas. Reinicie o Keycloak para recriar com KEYCLOAK_ADMIN/KEYCLOAK_ADMIN_PASSWORD' as status;





