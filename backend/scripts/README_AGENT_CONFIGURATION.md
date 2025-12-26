# Script de Inserção do Prompt do Agente

## Descrição

Este script SQL (`InsertAgentConfigurationPrompt.sql`) insere o prompt padrão de configuração do agente na tabela `AgentConfigurations`.

## Quando Usar

Execute este script após:
1. Criar a migration da tabela `AgentConfigurations`
2. Aplicar a migration no banco de dados
3. Antes de usar a funcionalidade de OCR pela primeira vez

## Como Executar

### Opção 1: SQL Server Management Studio (SSMS)

1. Abra o SSMS e conecte-se ao banco de dados `AssistenteExecutivo`
2. Abra o arquivo `InsertAgentConfigurationPrompt.sql`
3. Execute o script (F5 ou botão Execute)

### Opção 2: Linha de Comando (sqlcmd)

```bash
sqlcmd -S localhost -d AssistenteExecutivo -i InsertAgentConfigurationPrompt.sql
```

### Opção 3: Azure Data Studio / VS Code

1. Abra o arquivo `InsertAgentConfigurationPrompt.sql`
2. Conecte-se ao banco de dados
3. Execute o script

## O que o Script Faz

1. **Verifica se a tabela existe**: Se a tabela `AgentConfigurations` não existir, o script retorna um erro
2. **Verifica se já existe configuração**: Se já houver uma configuração, o script mostra uma mensagem e não insere duplicatas
3. **Insere o prompt padrão**: Se não existir configuração, insere o prompt padrão usado pelo serviço de refinamento OCR

## Prompt Padrão

O prompt padrão contém instruções detalhadas para a IA sobre como:
- Extrair informações de cartões de visita
- Fazer correção ortográfica mínima
- Validar e normalizar telefones e emails
- Validar o nome da empresa usando o domínio do email
- Retornar apenas JSON válido

## Editar o Prompt

Após inserir o prompt inicial, você pode editá-lo através da interface web:
- Acesse `/configuracoes-agente` no dashboard
- Edite o prompt conforme necessário
- Salve as alterações

## Notas Importantes

- O script usa `NEWID()` para gerar um GUID único para `ConfigurationId`
- As datas `CreatedAt` e `UpdatedAt` são definidas como `GETUTCDATE()`
- O prompt é armazenado como `NVARCHAR(MAX)` para suportar textos longos
- O script é idempotente: pode ser executado múltiplas vezes sem criar duplicatas

## Troubleshooting

### Erro: "A tabela AgentConfigurations não foi encontrada"
- **Solução**: Execute a migration primeiro:
  ```bash
  dotnet ef database update --project backend/src/AssistenteExecutivo.Infrastructure --startup-project backend/src/AssistenteExecutivo.Api
  ```

### Erro: "Já existe uma configuração"
- **Solução**: Isso é normal. O script não insere duplicatas. Se quiser atualizar, use a interface web ou faça um UPDATE manual.

### O prompt não está sendo usado
- **Solução**: Verifique se o serviço `QwenOcrRefinementService` está configurado corretamente e se o repositório está registrado no `DependencyInjection.cs`


