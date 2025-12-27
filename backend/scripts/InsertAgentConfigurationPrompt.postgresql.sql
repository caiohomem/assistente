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
               LEFT("OcrPrompt", 100) || '...' AS "OcrPromptPreview",
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
    
    RAISE NOTICE 'Prompts padrão inseridos com sucesso!';
    RAISE NOTICE '  - OcrPrompt: para extração de informações de cartões de visita';
    RAISE NOTICE '  - TranscriptionPrompt: para processamento de notas de áudio';
    RAISE NOTICE '';
    RAISE NOTICE 'ConfigurationId: %', v_configuration_id;
    RAISE NOTICE 'CreatedAt: %', v_created_at;
    RAISE NOTICE 'UpdatedAt: %', v_updated_at;
    RAISE NOTICE '';
    RAISE NOTICE 'Você pode editar estes prompts através da interface web em /configuracoes-agente';
    RAISE NOTICE '';
END $$;

