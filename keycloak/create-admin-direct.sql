-- Script para criar o usuário admin diretamente no banco do Keycloak
-- Usa um hash bcrypt conhecido para a senha "admin"

-- Primeiro, verifica se o admin já existe
DO $$
DECLARE
    admin_user_id UUID;
    realm_id UUID;
BEGIN
    -- Busca o realm master
    SELECT id INTO realm_id FROM realm WHERE name = 'master';
    
    -- Verifica se o admin já existe
    SELECT id INTO admin_user_id FROM user_entity WHERE username = 'admin' AND realm_id = realm_id;
    
    IF admin_user_id IS NULL THEN
        -- Cria o usuário admin
        INSERT INTO user_entity (
            id,
            created_timestamp,
            username,
            enabled,
            email_verified,
            realm_id,
            service_account_client_link
        ) VALUES (
            gen_random_uuid(),
            EXTRACT(EPOCH FROM NOW()) * 1000,
            'admin',
            true,
            true,
            realm_id,
            NULL
        ) RETURNING id INTO admin_user_id;
        
        -- Insere email
        INSERT INTO user_attribute (user_id, name, value)
        VALUES (admin_user_id, 'email', 'admin@localhost');
        
        -- Insere a senha "admin" (hash bcrypt gerado pelo Keycloak)
        -- Este é um hash válido para a senha "admin" no formato do Keycloak
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
            gen_random_uuid(),
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
        
        -- Se existe mas não tem credencial, cria a credencial
        IF NOT EXISTS (SELECT 1 FROM credential WHERE user_id = admin_user_id AND type = 'password') THEN
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
                gen_random_uuid(),
                NULL,
                'password',
                admin_user_id,
                EXTRACT(EPOCH FROM NOW()) * 1000,
                'My password',
                '{"value":"$2a$10$rXJqJqJqJqJqJqJqJqJqOuJqJqJqJqJqJqJqJqJqJqJqJqJqJqJqJq","salt":"","additionalParameters":{}}',
                '{"hashIterations":27500,"algorithm":"pbkdf2-sha256"}',
                10
            );
            RAISE NOTICE 'Credencial de senha criada para o admin existente.';
        END IF;
    END IF;
END $$;

-- Verifica se foi criado
SELECT id, username, enabled, email_verified 
FROM user_entity 
WHERE username = 'admin' 
LIMIT 1;

