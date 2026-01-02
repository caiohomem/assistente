-- Reset AgentConfigurations and insert default prompts (PostgreSQL).
-- This script deletes all rows and inserts a fresh configuration including WorkflowPrompt.
-- It also fixes legacy workflows with Manual triggers.

DO $$
DECLARE
    v_configuration_id UUID := gen_random_uuid();
    v_created_at TIMESTAMP := NOW();
    v_updated_at TIMESTAMP := NOW();
    v_exists BOOLEAN;
BEGIN
    -- Ensure table exists
    SELECT EXISTS (
        SELECT FROM information_schema.tables
        WHERE table_schema = 'public'
        AND table_name = 'AgentConfigurations'
    ) INTO v_exists;

    IF NOT v_exists THEN
        RAISE EXCEPTION 'ERRO: A tabela AgentConfigurations nao foi encontrada. Execute as migrations primeiro.';
    END IF;

    -- Ensure column exists
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
        AND table_name = 'AgentConfigurations'
        AND column_name = 'WorkflowPrompt'
    ) THEN
        ALTER TABLE "AgentConfigurations"
            ADD COLUMN "WorkflowPrompt" text;
    END IF;

    -- Delete existing configuration(s)
    DELETE FROM "AgentConfigurations";

    -- Insert default prompts
    INSERT INTO "AgentConfigurations" (
        "ConfigurationId",
        "OcrPrompt",
        "TranscriptionPrompt",
        "WorkflowPrompt",
        "CreatedAt",
        "UpdatedAt"
    )
    VALUES (
        v_configuration_id,
        'Analise esta imagem de um cartao de visita e extraia as seguintes informacoes em formato JSON valido:
{
  "name": "nome completo da pessoa",
  "email": "endereco de email",
  "phone": "telefone (formato brasileiro, apenas numeros)",
  "company": "nome da empresa",
  "jobTitle": "cargo/funcao",
  "rawText": "todo o texto visivel no cartao"
}

REGRAS:
- Extraia apenas informacoes que estejam claramente visiveis na imagem. Se algum campo nao estiver presente, use null.
- Para o telefone, normalize para formato brasileiro (apenas numeros, sem espacos ou caracteres especiais). Se houver +55, remova.
- Para o rawText, extraia TODO o texto visivel no cartao, linha por linha.
- CORRECAO ORTOGRAFICA: O texto pode conter erros de OCR. Faca correcao ortografica MINIMA apenas para corrigir erros obvios de reconhecimento de caracteres, mantendo o maximo possivel do texto original.
  Exemplos: "Secretria" -> "Secretaria", "Teanologia" -> "Tecnologia".
  NAO altere nomes proprios, URLs, emails ou numeros de telefone (exceto para normalizar formato).
- VALIDACAO DO NOME DA EMPRESA: Se houver um email no texto, extraia o dominio do email (parte apos o @, antes do .com/.com.br/etc) e use-o como referencia para validar e corrigir o nome da empresa.
  Exemplo: se o email e "joao@spacemoon.com.br" e o texto mostra "SPACEMOn", corrija para "SpaceMoon".
- Retorne APENAS o JSON, sem markdown, sem explicacoes adicionais.',
        'Analise a seguinte transcricao de uma nota de audio sobre um contato e organize as informacoes de forma estruturada.

Extraia e organize as informacoes em formato JSON valido com a seguinte estrutura:
{
  "summary": "resumo conciso em 2-3 frases do conteudo principal",
  "suggestions": [
    "sugestao de acao 1",
    "sugestao de acao 2"
  ]
}

Retorne APENAS o JSON, sem markdown, sem explicacoes adicionais.',
        $prompt$You are creating workflow specs for Assistente Executivo.

Rules:
- Always include a trigger. Manual trigger is NOT allowed.
- Allowed trigger types: Webhook, Scheduled, EventBased.

Start button (Workflows screen):
- If the flow must be started by the Start button, use Webhook.
- Set trigger.eventName to a unique, URL-safe path (ex: "workflow.start.<slug>").

Scheduled flows:
- Use Scheduled trigger.
- Must set trigger.cronExpression (5-field cron, UTC). Never leave cron empty.

Event-based flows:
- Use EventBased trigger.
- Set trigger.eventName to a domain event name.
- Examples: ContactCreated, ContactUpdated, ContactDeleted, DraftCreated, DraftApproved, DraftSent,
  ReminderScheduled, ReminderStatusChanged, NoteCreated, TemplateCreated, LetterheadCreated,
  CaptureJobRequested, CaptureJobCompleted, CaptureJobFailed, CreditsGranted, CreditsConsumed,
  CreditsRefunded, UserLoggedIn, UserProvisionedFromKeycloak, UserSuspended, UserReactivated,
  WorkflowActivated, WorkflowExecutionStarted, WorkflowExecutionCompleted, WorkflowExecutionFailed,
  WorkflowApprovalRequired, WorkflowStepApproved, WorkflowSpecUpdated.

Never output Manual trigger in specJson.$prompt$,
        v_created_at,
        v_updated_at
    );
END $$;

-- Fix legacy manual workflows to Webhook so they can be activated/executed.
WITH target AS (
  SELECT "WorkflowId",
         COALESCE("TriggerEventName", 'workflow-' || "WorkflowId"::text) AS event_name
  FROM "Workflows"
  WHERE "TriggerType" = 1
)
UPDATE "Workflows" w
SET "TriggerType" = 4,
    "TriggerEventName" = t.event_name,
    "TriggerCronExpression" = NULL,
    "SpecJson" = jsonb_set(
        jsonb_set(
            jsonb_set(w."SpecJson", '{trigger,type}', '"Webhook"', true),
            '{trigger,eventName}', to_jsonb(t.event_name), true),
        '{trigger,cronExpression}', 'null'::jsonb, true),
    "UpdatedAt" = NOW()
FROM target t
WHERE w."WorkflowId" = t."WorkflowId";
