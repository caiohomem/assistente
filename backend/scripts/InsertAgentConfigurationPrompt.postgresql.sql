-- Script SQL para inserir o prompt padrão de configuração do agente (PostgreSQL)
-- Este script:
-- 1. Verifica se a tabela AgentConfigurations existe
-- 2. Verifica se já existe uma configuração
-- 3. Insere o prompt padrão se não existir
--
-- IMPORTANTE: Certifique-se de estar conectado ao banco de dados correto
-- Execute este script após criar a migration da tabela AgentConfigurations

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
        RAISE EXCEPTION 'ERRO: A tabela AgentConfigurations não foi encontrada. Certifique-se de que as migrations foram executadas.';
    END IF;
    
    RAISE NOTICE '========================================';
    RAISE NOTICE 'Script de Inserção do Prompt do Agente';
    RAISE NOTICE '========================================';
    RAISE NOTICE '';
    
    -- Verificar se já existe uma configuração
    SELECT EXISTS (SELECT 1 FROM "AgentConfigurations") INTO v_exists;
    
    IF v_exists THEN
        RAISE NOTICE 'AVISO: Já existe uma configuração na tabela AgentConfigurations.';
        RAISE NOTICE 'Para atualizar, use a interface web ou execute um UPDATE manual.';
        RAISE NOTICE '';
        RAISE NOTICE 'Configuração existente:';
        
        PERFORM "ConfigurationId",
               LEFT("ContextPrompt", 100) || '...' AS "ContextPromptPreview",
               "CreatedAt",
               "UpdatedAt"
        FROM "AgentConfigurations";
        
        RETURN;
    END IF;
    
    RAISE NOTICE 'Inserindo prompt padrão...';
    RAISE NOTICE '';
    
    -- Inserir o prompt padrão
    INSERT INTO "AgentConfigurations" (
        "ConfigurationId",
        "ContextPrompt",
        "CreatedAt",
        "UpdatedAt"
    )
    VALUES (
        v_configuration_id,
        'Analise o seguinte texto extraído de um cartão de visita e identifique os campos solicitados.

Extraia e retorne APENAS um JSON válido com esta estrutura:
{
  "name": "nome completo da pessoa ou null",
  "email": "email ou null",
  "phone": "telefone (apenas dígitos com DDD, sem +55) ou null",
  "company": "nome da empresa ou null",
  "jobTitle": "cargo/função ou null"
}

REGRAS:
- NUNCA invente informações. Se não encontrar, use null.
- CORREÇÃO ORTOGRÁFICA: O texto pode conter erros de OCR (letras mal reconhecidas). Faça correção ortográfica MÍNIMA apenas para corrigir erros óbvios de reconhecimento de caracteres, mantendo o máximo possível do texto original.
  Exemplos de correções: "Secretria" -> "Secretaria", "Teanologia" -> "Tecnologia", "Inovacäo" -> "Inovação", "Cisnoia" -> "Ciência", "Transformagäo" -> "Transformação".
  NÃO altere nomes próprios, URLs, emails ou números de telefone (exceto para normalizar formato).
- Os valores devem corresponder ao texto original (após correção ortográfica mínima).
- Telefone: apenas dígitos (DDD + número, 10 ou 11 dígitos). Se houver +55, remova. Normalize removendo espaços, pontos e hífens, mas mantenha apenas os dígitos.
- Email: formato válido de email. Mantenha exatamente como aparece, apenas corrija erros óbvios de OCR se necessário.
- Nome: geralmente aparece no topo do cartão, pode ter 2-5 palavras. Corrija apenas erros óbvios de OCR.
- Empresa: geralmente aparece abaixo do nome ou em linha separada. Corrija apenas erros óbvios de OCR.
  **VALIDAÇÃO CRÍTICA DO NOME DA EMPRESA**: Se houver um email no texto, SEMPRE extraia o domínio do email (parte após o @, antes do .com/.com.br/etc) e use-o como referência para validar e corrigir o nome da empresa. Quase sempre o nome da empresa aparece no domínio do email. 
  Exemplo: se o email é "joao@spacemoon.com.br" e o texto OCR mostra "SPACEMOn", corrija para "SpaceMoon" (ou a grafia correta baseada no domínio "spacemoon"). 
  Se o nome da empresa no texto não corresponder ao domínio do email, PREFIRA usar a grafia do domínio do email como fonte confiável, ajustando apenas capitalização se necessário.
- Cargo: geralmente aparece entre nome e empresa, ou abaixo do nome. Corrija apenas erros óbvios de OCR.
- Retorne SOMENTE o JSON, sem markdown, sem explicações, sem texto adicional.',
        v_created_at,
        v_updated_at
    );
    
    RAISE NOTICE 'Prompt padrão inserido com sucesso!';
    RAISE NOTICE '';
    RAISE NOTICE 'ConfigurationId: %', v_configuration_id;
    RAISE NOTICE 'CreatedAt: %', v_created_at;
    RAISE NOTICE 'UpdatedAt: %', v_updated_at;
    RAISE NOTICE '';
    RAISE NOTICE 'Você pode editar este prompt através da interface web em /configuracoes-agente';
    RAISE NOTICE '';
END $$;

