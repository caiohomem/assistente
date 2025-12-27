-- ============================================================================
-- SCRIPT COMPLETO DE INICIALIZAÇÃO DO BANCO DE DADOS POSTGRESQL
-- ============================================================================
-- Este script executa TODOS os passos necessários para inicializar o banco:
-- 1. Remove todas as tabelas existentes (limpa o banco) - OPCIONAL
-- 2. Insere dados iniciais (AgentConfiguration, CreditPackages)
--
-- NOTA: SessionCache e Logs não são mais necessários:
-- - SessionCache: Usamos Redis para cache de sessão
-- - Logs: Usamos Google Cloud Console para logs em produção
--
-- IMPORTANTE: 
-- - Execute este script APÓS aplicar as migrations do EF Core:
--   cd backend/src/AssistenteExecutivo.Api
--   dotnet ef database update
--
-- - Ou execute este script ANTES das migrations se quiser limpar o banco primeiro
--   (neste caso, as tabelas principais serão criadas pelas migrations)
--
-- ============================================================================

\echo '========================================'
\echo 'INICIALIZAÇÃO DO BANCO DE DADOS'
\echo '========================================'
\echo ''

-- ============================================================================
-- PARTE 1: LIMPAR BANCO DE DADOS (OPCIONAL)
-- ============================================================================
-- Descomente as linhas abaixo se quiser limpar o banco antes de inicializar
-- ATENÇÃO: Isso irá remover TODAS as tabelas e dados!

/*
\echo 'Limpando banco de dados...'
DROP SCHEMA public CASCADE;
CREATE SCHEMA public;

-- Restaurar permissões padrão
GRANT ALL ON SCHEMA public TO postgres;
GRANT ALL ON SCHEMA public TO public;
\echo 'Banco de dados limpo!'
\echo ''
*/

-- ============================================================================
-- PARTE 2: INSERIR DADOS INICIAIS
-- ============================================================================

\echo 'Inserindo dados iniciais...'
\echo ''

-- 3.1: Configuração do Agente (AgentConfiguration)
DO $$
DECLARE
    v_configuration_id UUID := gen_random_uuid();
    v_created_at TIMESTAMP := NOW();
    v_updated_at TIMESTAMP := NOW();
    v_exists BOOLEAN;
BEGIN
    -- Verificar se a tabela existe
    SELECT EXISTS (
        SELECT FROM information_schema.tables 
        WHERE table_schema = 'public' 
        AND table_name = 'AgentConfigurations'
    ) INTO v_exists;
    
    IF NOT v_exists THEN
        RAISE WARNING 'Tabela AgentConfigurations não encontrada. Execute as migrations primeiro: dotnet ef database update';
        RETURN;
    END IF;
    
    -- Verificar se já existe uma configuração
    SELECT EXISTS (SELECT 1 FROM "AgentConfigurations") INTO v_exists;
    
    IF v_exists THEN
        RAISE NOTICE 'AVISO: Já existe uma configuração na tabela AgentConfigurations. Pulando inserção.';
        RETURN;
    END IF;
    
    -- Inserir o prompt padrão
    INSERT INTO "AgentConfigurations" (
        "ConfigurationId",
        "OcrPrompt",
        "TranscriptionPrompt",
        "CreatedAt",
        "UpdatedAt"
    )
    VALUES (
        v_configuration_id,
        'Analise esta imagem de um cartão de visita e extraia as seguintes informações em formato JSON válido:
{
  "name": "nome completo da pessoa",
  "email": "endereço de email",
  "phone": "telefone (formato brasileiro, apenas números)",
  "company": "nome da empresa",
  "jobTitle": "cargo/função",
  "rawText": "todo o texto visível no cartão"
}

REGRAS:
- Extraia apenas informações que estejam claramente visíveis na imagem. Se algum campo não estiver presente, use null.
- Para o telefone, normalize para formato brasileiro (apenas números, sem espaços ou caracteres especiais). Se houver +55, remova.
- Para o rawText, extraia TODO o texto visível no cartão, linha por linha.
- CORREÇÃO ORTOGRÁFICA: O texto pode conter erros de OCR. Faça correção ortográfica MÍNIMA apenas para corrigir erros óbvios de reconhecimento de caracteres, mantendo o máximo possível do texto original.
  Exemplos: "Secretria" -> "Secretaria", "Teanologia" -> "Tecnologia".
  NÃO altere nomes próprios, URLs, emails ou números de telefone (exceto para normalizar formato).
- **VALIDAÇÃO DO NOME DA EMPRESA**: Se houver um email no texto, extraia o domínio do email (parte após o @, antes do .com/.com.br/etc) e use-o como referência para validar e corrigir o nome da empresa.
  Exemplo: se o email é "joao@spacemoon.com.br" e o texto mostra "SPACEMOn", corrija para "SpaceMoon".
- Retorne APENAS o JSON, sem markdown, sem explicações adicionais.',
        'Analise a seguinte transcrição de uma nota de áudio sobre um contato e organize as informações de forma estruturada.

Extraia e organize as informações em formato JSON válido com a seguinte estrutura:
{
  "summary": "resumo conciso em 2-3 frases do conteúdo principal",
  "suggestions": [
    "sugestão de ação 1",
    "sugestão de ação 2"
  ]
}

Retorne APENAS o JSON, sem markdown, sem explicações adicionais.',
        v_created_at,
        v_updated_at
    );
    
    RAISE NOTICE 'Configuração do agente inserida com sucesso! (ConfigurationId: %)', v_configuration_id;
END $$;

-- 3.2: Packages de Créditos (CreditPackages)
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
        RAISE WARNING 'Tabela CreditPackages não encontrada. Execute as migrations primeiro: dotnet ef database update';
        RETURN;
    END IF;
    
    -- Verificar se já existem packages
    SELECT EXISTS (SELECT 1 FROM "CreditPackages") INTO v_exists;
    
    IF v_exists THEN
        RAISE NOTICE 'AVISO: Já existem packages na tabela CreditPackages. Pulando inserção.';
        RETURN;
    END IF;
    
    -- Package 1: Pacote Básico - 100 créditos
    v_package_id := gen_random_uuid();
    INSERT INTO "CreditPackages" (
        "PackageId", "Name", "Amount", "Price", "Currency", "Description", "IsActive", "CreatedAt", "UpdatedAt"
    ) VALUES (
        v_package_id, 'Pacote Básico', 100, 9.90, 'BRL', '100 créditos para processamento de cartões de visita e notas de áudio', true, v_created_at, v_updated_at
    );
    RAISE NOTICE 'Package inserido: Pacote Básico (100 créditos) - R$ 9,90';
    
    -- Package 2: Pacote Intermediário - 500 créditos
    v_package_id := gen_random_uuid();
    INSERT INTO "CreditPackages" (
        "PackageId", "Name", "Amount", "Price", "Currency", "Description", "IsActive", "CreatedAt", "UpdatedAt"
    ) VALUES (
        v_package_id, 'Pacote Intermediário', 500, 39.90, 'BRL', '500 créditos para processamento de cartões de visita e notas de áudio', true, v_created_at, v_updated_at
    );
    RAISE NOTICE 'Package inserido: Pacote Intermediário (500 créditos) - R$ 39,90';
    
    -- Package 3: Pacote Avançado - 1000 créditos
    v_package_id := gen_random_uuid();
    INSERT INTO "CreditPackages" (
        "PackageId", "Name", "Amount", "Price", "Currency", "Description", "IsActive", "CreatedAt", "UpdatedAt"
    ) VALUES (
        v_package_id, 'Pacote Avançado', 1000, 69.90, 'BRL', '1000 créditos para processamento de cartões de visita e notas de áudio', true, v_created_at, v_updated_at
    );
    RAISE NOTICE 'Package inserido: Pacote Avançado (1000 créditos) - R$ 69,90';
    
    -- Package 4: Pacote Profissional - 2500 créditos
    v_package_id := gen_random_uuid();
    INSERT INTO "CreditPackages" (
        "PackageId", "Name", "Amount", "Price", "Currency", "Description", "IsActive", "CreatedAt", "UpdatedAt"
    ) VALUES (
        v_package_id, 'Pacote Profissional', 2500, 149.90, 'BRL', '2500 créditos para processamento de cartões de visita e notas de áudio', true, v_created_at, v_updated_at
    );
    RAISE NOTICE 'Package inserido: Pacote Profissional (2500 créditos) - R$ 149,90';
    
    -- Package 5: Pacote Empresarial - 5000 créditos
    v_package_id := gen_random_uuid();
    INSERT INTO "CreditPackages" (
        "PackageId", "Name", "Amount", "Price", "Currency", "Description", "IsActive", "CreatedAt", "UpdatedAt"
    ) VALUES (
        v_package_id, 'Pacote Empresarial', 5000, 249.90, 'BRL', '5000 créditos para processamento de cartões de visita e notas de áudio', true, v_created_at, v_updated_at
    );
    RAISE NOTICE 'Package inserido: Pacote Empresarial (5000 créditos) - R$ 249,90';
    
    RAISE NOTICE 'Packages de créditos inseridos com sucesso! (Total: 5 packages)';
END $$;

\echo ''
\echo '========================================'
\echo 'INICIALIZAÇÃO CONCLUÍDA!'
\echo '========================================'
\echo ''
\echo 'Próximos passos:'
\echo '1. Se ainda não aplicou as migrations, execute:'
\echo '   cd backend/src/AssistenteExecutivo.Api'
\echo '   dotnet ef database update'
\echo ''
\echo '2. Inicie a aplicação para verificar se tudo está funcionando'
\echo ''

