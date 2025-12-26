-- Script SQL para inserir packages de créditos padrão (PostgreSQL)
-- Este script:
-- 1. Verifica se a tabela CreditPackages existe
-- 2. Verifica se já existem packages
-- 3. Insere packages padrão se não existirem
--
-- IMPORTANTE: Certifique-se de estar conectado ao banco de dados correto
-- Execute este script após criar a migration da tabela CreditPackages

DO $$
DECLARE
    v_package_id UUID;
    v_created_at TIMESTAMP := NOW();
    v_updated_at TIMESTAMP := NOW();
    v_exists BOOLEAN;
BEGIN
    -- Verificar se a tabela existe
    SELECT EXISTS (
        SELECT FROM information_schema.tables 
        WHERE table_schema = 'public' 
        AND table_name = 'CreditPackages'
    ) INTO v_exists;
    
    IF NOT v_exists THEN
        RAISE EXCEPTION 'ERRO: A tabela CreditPackages não foi encontrada. Certifique-se de que as migrations foram executadas.';
    END IF;
    
    RAISE NOTICE '========================================';
    RAISE NOTICE 'Script de Inserção de Packages de Créditos';
    RAISE NOTICE '========================================';
    RAISE NOTICE '';
    
    -- Verificar se já existem packages
    SELECT EXISTS (SELECT 1 FROM "CreditPackages") INTO v_exists;
    
    IF v_exists THEN
        RAISE NOTICE 'AVISO: Já existem packages na tabela CreditPackages.';
        RAISE NOTICE 'Para atualizar, use a interface web ou execute um UPDATE manual.';
        RAISE NOTICE '';
        RAISE NOTICE 'Packages existentes encontrados.';
        RAISE NOTICE 'Execute: SELECT * FROM "CreditPackages"; para ver os packages.';
        
        RETURN;
    END IF;
    
    RAISE NOTICE 'Inserindo packages padrão...';
    RAISE NOTICE '';
    
    -- Package 1: Pacote Básico - 100 créditos
    v_package_id := gen_random_uuid();
    INSERT INTO "CreditPackages" (
        "PackageId",
        "Name",
        "Amount",
        "Price",
        "Currency",
        "Description",
        "IsActive",
        "CreatedAt",
        "UpdatedAt"
    )
    VALUES (
        v_package_id,
        'Pacote Básico',
        100,
        9.90,
        'BRL',
        '100 créditos para processamento de cartões de visita e notas de áudio',
        true,
        v_created_at,
        v_updated_at
    );
    RAISE NOTICE 'Package inserido: Pacote Básico (100 créditos) - R$ 9,90';
    
    -- Package 2: Pacote Intermediário - 500 créditos
    v_package_id := gen_random_uuid();
    INSERT INTO "CreditPackages" (
        "PackageId",
        "Name",
        "Amount",
        "Price",
        "Currency",
        "Description",
        "IsActive",
        "CreatedAt",
        "UpdatedAt"
    )
    VALUES (
        v_package_id,
        'Pacote Intermediário',
        500,
        39.90,
        'BRL',
        '500 créditos para processamento de cartões de visita e notas de áudio',
        true,
        v_created_at,
        v_updated_at
    );
    RAISE NOTICE 'Package inserido: Pacote Intermediário (500 créditos) - R$ 39,90';
    
    -- Package 3: Pacote Avançado - 1000 créditos
    v_package_id := gen_random_uuid();
    INSERT INTO "CreditPackages" (
        "PackageId",
        "Name",
        "Amount",
        "Price",
        "Currency",
        "Description",
        "IsActive",
        "CreatedAt",
        "UpdatedAt"
    )
    VALUES (
        v_package_id,
        'Pacote Avançado',
        1000,
        69.90,
        'BRL',
        '1000 créditos para processamento de cartões de visita e notas de áudio',
        true,
        v_created_at,
        v_updated_at
    );
    RAISE NOTICE 'Package inserido: Pacote Avançado (1000 créditos) - R$ 69,90';
    
    -- Package 4: Pacote Profissional - 2500 créditos
    v_package_id := gen_random_uuid();
    INSERT INTO "CreditPackages" (
        "PackageId",
        "Name",
        "Amount",
        "Price",
        "Currency",
        "Description",
        "IsActive",
        "CreatedAt",
        "UpdatedAt"
    )
    VALUES (
        v_package_id,
        'Pacote Profissional',
        2500,
        149.90,
        'BRL',
        '2500 créditos para processamento de cartões de visita e notas de áudio',
        true,
        v_created_at,
        v_updated_at
    );
    RAISE NOTICE 'Package inserido: Pacote Profissional (2500 créditos) - R$ 149,90';
    
    -- Package 5: Pacote Empresarial - 5000 créditos
    v_package_id := gen_random_uuid();
    INSERT INTO "CreditPackages" (
        "PackageId",
        "Name",
        "Amount",
        "Price",
        "Currency",
        "Description",
        "IsActive",
        "CreatedAt",
        "UpdatedAt"
    )
    VALUES (
        v_package_id,
        'Pacote Empresarial',
        5000,
        249.90,
        'BRL',
        '5000 créditos para processamento de cartões de visita e notas de áudio',
        true,
        v_created_at,
        v_updated_at
    );
    RAISE NOTICE 'Package inserido: Pacote Empresarial (5000 créditos) - R$ 249,90';
    
    RAISE NOTICE '';
    RAISE NOTICE '========================================';
    RAISE NOTICE 'Packages de créditos inseridos com sucesso!';
    RAISE NOTICE 'Total: 5 packages';
    RAISE NOTICE '========================================';
    RAISE NOTICE '';
    RAISE NOTICE 'Você pode editar estes packages através da interface web';
    RAISE NOTICE '';
END $$;

