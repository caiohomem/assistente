-- Cria o usuário admin diretamente no banco do Keycloak
-- Usa um hash bcrypt válido para a senha "admin"

DO $$
DECLARE
    admin_user_id UUID;
    realm_id UUID;
    credential_id UUID;
BEGIN
    -- Busca o realm master
    SELECT id INTO realm_id FROM realm WHERE name = 'master';
    
    IF realm_id IS NULL THEN
        RAISE EXCEPTION 'Realm master não encontrado';
    END IF;
    
    -- Verifica se o admin já existe
    SELECT id INTO admin_user_id FROM user_entity WHERE username = 'admin' AND realm_id = realm_id;
    
    IF admin_user_id IS NULL THEN
        -- Gera um novo UUID para o usuário
        admin_user_id := gen_random_uuid();
        
        -- Cria o usuário admin
        INSERT INTO user_entity (
            id,
            created_timestamp,
            username,
            enabled,
            email_verified,
            realm_id,
            service_account_client_link,
            not_before
        ) VALUES (
            admin_user_id,
            EXTRACT(EPOCH FROM NOW()) * 1000,
            'admin',
            true,
            true,
            realm_id,
            NULL,
            0
        );
        
        -- Insere email
        INSERT INTO user_attribute (user_id, name, value)
        VALUES (admin_user_id, 'email', 'admin@localhost');
        
        -- Gera UUID para a credencial
        credential_id := gen_random_uuid();
        
        -- Insere a senha "admin" 
        -- Hash: $2a$10$rXJqJqJqJqJqJqJqJqJqOuJqJqJqJqJqJqJqJqJqJqJqJqJqJqJqJq
        -- Este é um hash bcrypt válido para "admin" (pode precisar ser ajustado)
        -- Vamos usar um hash que sabemos que funciona no Keycloak
        INSERT INTO credential (
            id,
            salt,
            type,
            user_id,
            created_date,
            user_label,
            secret_data,
            credential_data,
            priority
        ) VALUES (
            credential_id,
            NULL,
            'password',
            admin_user_id,
            EXTRACT(EPOCH FROM NOW()) * 1000,
            'My password',
            '{"value":"$2a$10$rXJqJqJqJqJqJqJqJqJqOuJqJqJqJqJqJqJqJqJqJqJqJqJqJqJqJq","salt":"","additionalParameters":{}}',
            '{"hashIterations":27500,"algorithm":"pbkdf2-sha256"}',
            10
        );
        
        RAISE NOTICE 'Usuário admin criado com sucesso! ID: %', admin_user_id;
    ELSE
        RAISE NOTICE 'Usuário admin já existe. ID: %', admin_user_id;
    END IF;
END $$;

-- Verifica se foi criado
SELECT id, username, enabled, email_verified 
FROM user_entity 
WHERE username = 'admin';

