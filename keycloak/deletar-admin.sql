-- Deleta o admin para recriar com hash válido
DELETE FROM credential WHERE user_id IN (SELECT id FROM user_entity WHERE username = 'admin');
DELETE FROM user_attribute WHERE user_id IN (SELECT id FROM user_entity WHERE username = 'admin');
DELETE FROM user_entity WHERE username = 'admin';

SELECT 'Admin deletado. Keycloak deve criar automaticamente com KC_BOOTSTRAP_ADMIN_USERNAME e KC_BOOTSTRAP_ADMIN_PASSWORD.' as status;





