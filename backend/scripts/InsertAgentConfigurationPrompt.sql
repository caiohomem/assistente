-- Script SQL para inserir o prompt padrão de configuração do agente
-- Este script:
-- 1. Verifica se a tabela AgentConfigurations existe
-- 2. Verifica se já existe uma configuração
-- 3. Insere o prompt padrão se não existir
--
-- IMPORTANTE: Certifique-se de estar conectado ao banco de dados correto
-- Execute este script após criar a migration da tabela AgentConfigurations

-- Verificar se a tabela existe
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AgentConfigurations')
BEGIN
    RAISERROR('ERRO: A tabela AgentConfigurations não foi encontrada. Certifique-se de que as migrations foram executadas.', 16, 1);
    RETURN;
END

PRINT '========================================';
PRINT 'Script de Inserção do Prompt do Agente';
PRINT '========================================';
PRINT '';

DECLARE @ConfigurationId UNIQUEIDENTIFIER = NEWID();
DECLARE @CreatedAt DATETIME = GETUTCDATE();
DECLARE @UpdatedAt DATETIME = GETUTCDATE();

-- Verificar se já existe uma configuração
IF EXISTS (SELECT 1 FROM AgentConfigurations)
BEGIN
    PRINT 'AVISO: Já existe uma configuração na tabela AgentConfigurations.';
    PRINT 'Para atualizar, use a interface web ou execute um UPDATE manual.';
    PRINT '';
    PRINT 'Configuração existente:';
    SELECT 
        ConfigurationId,
        LEFT(ContextPrompt, 100) + '...' AS ContextPromptPreview,
        CreatedAt,
        UpdatedAt
    FROM AgentConfigurations;
    RETURN;
END

PRINT 'Inserindo prompt padrão...';
PRINT '';

-- Inserir o prompt padrão
INSERT INTO AgentConfigurations (
    ConfigurationId,
    ContextPrompt,
    CreatedAt,
    UpdatedAt
)
VALUES (
    @ConfigurationId,
    N'Analise o seguinte texto extraído de um cartão de visita e identifique os campos solicitados.

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
    @CreatedAt,
    @UpdatedAt
);

PRINT 'Prompt padrão inserido com sucesso!';
PRINT '';
PRINT 'ConfigurationId: ' + CAST(@ConfigurationId AS VARCHAR(36));
PRINT 'CreatedAt: ' + CONVERT(VARCHAR, @CreatedAt, 120);
PRINT 'UpdatedAt: ' + CONVERT(VARCHAR, @UpdatedAt, 120);
PRINT '';
PRINT 'Você pode editar este prompt através da interface web em /configuracoes-agente';
PRINT '';




