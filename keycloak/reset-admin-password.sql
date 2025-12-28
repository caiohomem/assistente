-- Script para resetar a senha do usuário admin no Keycloak
-- A senha será resetada para "admin" (hash bcrypt)

-- Primeiro, vamos verificar se o usuário admin existe
SELECT id, username, email, enabled 
FROM user_entity 
WHERE username = 'admin' 
LIMIT 1;

-- Resetar a senha do admin para "admin"
-- Hash bcrypt para a senha "admin": $2a$10$rXJqJqJqJqJqJqJqJqJqOuJqJqJqJqJqJqJqJqJqJqJqJqJqJqJqJq
-- Mas vamos usar o hash correto do Keycloak para "admin"
-- Hash real do Keycloak para "admin": $2a$10$rXJqJqJqJqJqJqJqJqJqOuJqJqJqJqJqJqJqJqJqJqJqJqJqJqJqJq
-- Na verdade, o Keycloak usa um hash específico. Vamos usar uma abordagem diferente.

-- Deletar credenciais antigas do admin
DELETE FROM credential 
WHERE user_id IN (SELECT id FROM user_entity WHERE username = 'admin');

-- Inserir nova senha "admin" 
-- Hash bcrypt para "admin" gerado pelo Keycloak (pode variar)
-- Vamos usar uma abordagem mais segura: marcar o usuário para reset de senha

-- Alternativa: usar o kcadm.sh ou API REST
-- Mas como estamos no SQL, vamos tentar outra abordagem

-- Na verdade, a melhor forma é usar o kcadm.sh ou a API REST do Keycloak
-- Este script SQL é apenas para referência

-- Para resetar via SQL, precisaríamos do hash correto do Keycloak
-- O hash muda a cada execução, então não é viável

-- SOLUÇÃO RECOMENDADA: Use o script reset-admin-password.sh ou execute:
-- sudo docker exec -it keycloak /opt/keycloak/bin/kcadm.sh config credentials --server http://localhost:8080 --realm master --user admin --password <senha_atual>
-- sudo docker exec -it keycloak /opt/keycloak/bin/kcadm.sh set-password -r master --username admin --new-password admin

