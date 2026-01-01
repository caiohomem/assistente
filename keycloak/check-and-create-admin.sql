-- Verifica se o admin existe e cria se não existir

-- Verifica se existe
SELECT id, username, enabled 
FROM user_entity 
WHERE username = 'admin' 
LIMIT 1;

-- Se não existir, cria (executar manualmente se necessário)
-- O hash abaixo é um exemplo - precisamos gerar um hash válido do Keycloak



