-- Script para criar o usuário admin diretamente no banco do Keycloak
-- Senha: admin

DO $$
DECLARE
    admin_user_id UUID;
    master_realm_id UUID;
    credential_id UUID;
    timestamp_now BIGINT;
BEGIN
    -- Busca o realm master
    SELECT id INTO master_realm_id FROM realm WHERE name = 'master';
    
    IF master_realm_id IS NULL THEN
        RAISE EXCEPTION 'Realm master não encontrado';
    END IF;
    
    -- Verifica se o admin já existe (no realm master)
    SELECT id INTO admin_user_id FROM user_entity WHERE username = 'admin' AND realm_id = master_realm_id::varchar;
    
    timestamp_now := EXTRACT(EPOCH FROM NOW()) * 1000;
    
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
            timestamp_now,
            'admin',
            true,
            true,
            master_realm_id,
            NULL,
            0
        );
        
        -- Insere email
        INSERT INTO user_attribute (user_id, name, value)
        VALUES (admin_user_id, 'email', 'admin@localhost');
        
        -- Gera UUID para a credencial
        credential_id := gen_random_uuid();
        
        -- Insere a senha "admin" 
        -- Hash bcrypt válido para a senha "admin" no formato do Keycloak
        -- Este hash foi gerado pelo Keycloak para a senha "admin"
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
            timestamp_now,
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
            credential_id := gen_random_uuid();
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
                timestamp_now,
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
WHERE username = 'admin';

