-- Script de migração para remover campos desnecessários da tabela AgentConfigurations (PostgreSQL)
-- Remove: ContextPrompt, OcrRefinementPrompt
-- Torna OcrPrompt obrigatório (não nullable)
--
-- IMPORTANTE: Execute este script APÓS atualizar o código da aplicação
-- Este script migra dados existentes: se ContextPrompt existir, copia para OcrPrompt

DO $$
BEGIN
    IF EXISTS (
        SELECT FROM information_schema.tables 
        WHERE table_schema = 'public' 
        AND table_name = 'AgentConfigurations'
    ) THEN
        RAISE NOTICE 'Iniciando migração da tabela AgentConfigurations...';
        RAISE NOTICE '';

        -- 1. Se OcrPrompt estiver NULL e ContextPrompt existir, copiar ContextPrompt para OcrPrompt
        IF EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'AgentConfigurations' AND column_name = 'ContextPrompt') THEN
            UPDATE "AgentConfigurations"
            SET "OcrPrompt" = "ContextPrompt"
            WHERE "OcrPrompt" IS NULL AND "ContextPrompt" IS NOT NULL AND LENGTH("ContextPrompt") > 0;
            
            RAISE NOTICE 'Dados migrados: ContextPrompt -> OcrPrompt (se necessário)';
        END IF;

        -- 2. Tornar OcrPrompt obrigatório (não nullable)
        -- Primeiro garantir que não há NULLs
        IF EXISTS (SELECT 1 FROM "AgentConfigurations" WHERE "OcrPrompt" IS NULL) THEN
            -- Se ainda houver NULLs, usar prompt padrão
            UPDATE "AgentConfigurations"
            SET "OcrPrompt" = 'Analise esta imagem de um cartão de visita e extraia as seguintes informações em formato JSON válido...'
            WHERE "OcrPrompt" IS NULL;
            RAISE NOTICE 'Aviso: Alguns registros tinham OcrPrompt NULL. Foi aplicado prompt padrão.';
        END IF;

        -- 3. Remover colunas desnecessárias
        IF EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'AgentConfigurations' AND column_name = 'ContextPrompt') THEN
            ALTER TABLE "AgentConfigurations" DROP COLUMN "ContextPrompt";
            RAISE NOTICE 'Coluna ContextPrompt removida.';
        END IF;

        IF EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'AgentConfigurations' AND column_name = 'OcrRefinementPrompt') THEN
            ALTER TABLE "AgentConfigurations" DROP COLUMN "OcrRefinementPrompt";
            RAISE NOTICE 'Coluna OcrRefinementPrompt removida.';
        END IF;

        -- 4. Tornar OcrPrompt NOT NULL
        ALTER TABLE "AgentConfigurations"
        ALTER COLUMN "OcrPrompt" SET NOT NULL;
        
        RAISE NOTICE 'Coluna OcrPrompt agora é obrigatória (NOT NULL).';
        RAISE NOTICE '';
        RAISE NOTICE 'Migração concluída com sucesso!';
    ELSE
        RAISE EXCEPTION 'ERRO: A tabela AgentConfigurations não foi encontrada.';
    END IF;
END $$;







