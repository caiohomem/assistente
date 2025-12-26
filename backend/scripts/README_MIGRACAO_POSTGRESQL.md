# Migração para PostgreSQL (Neon)

Este documento descreve a migração do banco de dados de SQL Server para PostgreSQL (Neon).

## Alterações Realizadas

### 1. Pacotes NuGet Adicionados

- `Npgsql.EntityFrameworkCore.PostgreSQL` (v10.0.1) - Provider do Entity Framework para PostgreSQL
- `Serilog.Sinks.PostgreSQL` (v3.0.0) - Sink do Serilog para PostgreSQL
- `Microsoft.Extensions.Caching.StackExchangeRedis` (v10.0.1) - Para cache distribuído (opcional, caso queira usar Redis)

### 2. Código Atualizado

#### DependencyInjection.cs
- Detecta automaticamente se a connection string é PostgreSQL ou SQL Server
- Usa `UseNpgsql()` para PostgreSQL e `UseSqlServer()` para SQL Server (fallback)

#### Program.cs
- Configuração do Serilog adaptada para PostgreSQL
- Session Cache: usa Memory Cache quando detecta PostgreSQL (pois .NET não tem suporte nativo para PostgreSQL distributed cache)

### 3. Scripts SQL Convertidos

Todos os scripts foram convertidos de T-SQL (SQL Server) para PostgreSQL:

- `CreateLogsTable.postgresql.sql` - Cria a tabela de logs
- `CreateSessionCacheTable.postgresql.sql` - Cria a tabela de session cache (referência)
- `InsertAgentConfigurationPrompt.postgresql.sql` - Insere o prompt padrão do agente

## Como Usar

### 1. Configurar Connection String

Edite o arquivo `appsettings.Development.json` (ou `appsettings.json` para produção) e adicione sua connection string do Neon:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=seu-host.neon.tech;Database=seu-database;Username=seu-usuario;Password=sua-senha;SSL Mode=Require;"
  }
}
```

**Formato da connection string do Neon:**
```
Host=ep-xxx-xxx.region.aws.neon.tech;Database=neondb;Username=usuario;Password=senha;SSL Mode=Require;
```

### 2. Executar Scripts SQL

Execute os scripts PostgreSQL no banco de dados Neon:

1. **Criar tabela de logs:**
   ```sql
   -- Execute: backend/scripts/CreateLogsTable.postgresql.sql
   ```

2. **Inserir prompt padrão do agente:**
   ```sql
   -- Execute: backend/scripts/InsertAgentConfigurationPrompt.postgresql.sql
   ```

### 3. Executar Migrations do Entity Framework

As migrations do Entity Framework serão automaticamente aplicadas na primeira execução da aplicação. Se preferir executar manualmente:

```bash
dotnet ef database update --project backend/src/AssistenteExecutivo.Infrastructure --startup-project backend/src/AssistenteExecutivo.Api
```

### 4. Executar a Aplicação

A aplicação detectará automaticamente que está usando PostgreSQL pela connection string e configurará tudo adequadamente.

## Diferenças Importantes

### Session Cache

Quando usando PostgreSQL, a aplicação usa **Memory Cache** ao invés de Distributed Cache, pois o .NET não tem suporte nativo para PostgreSQL distributed cache. Isso significa que:

- ✅ Sessões funcionam normalmente dentro de uma única instância
- ⚠️ Sessões são perdidas quando a aplicação reinicia
- ⚠️ Múltiplas instâncias não compartilham sessões

**Solução para produção com múltiplas instâncias:**
- Use Redis para distributed cache
- Ou implemente um provider customizado para PostgreSQL

### Logs

**Nota Importante:** Atualmente, quando usando PostgreSQL, os logs são salvos apenas no **console**. A tabela `Logs` foi criada no banco de dados caso você queira implementar um provider customizado no futuro.

Para salvar logs no PostgreSQL, você pode:
1. Implementar um provider customizado usando Npgsql diretamente
2. Usar Serilog.Sinks.File e processar os logs depois
3. Usar um serviço de logging externo (ex: Seq, ELK Stack)

A estrutura da tabela `Logs` no PostgreSQL é similar ao SQL Server, mas com algumas diferenças:

- Colunas em minúsculas (padrão PostgreSQL)
- Tipo `JSONB` para a coluna `Properties`
- Tipo `TIMESTAMP` ao invés de `DATETIME2`

## Verificação

Para verificar se a migração foi bem-sucedida:

1. Execute a aplicação e verifique os logs no console
2. Verifique se as tabelas foram criadas no banco Neon
3. Teste as funcionalidades principais da aplicação

## Rollback

Se precisar voltar para SQL Server:

1. Altere a connection string no `appsettings.json` para SQL Server
2. Execute os scripts SQL Server originais
3. A aplicação detectará automaticamente e usará SQL Server

## Notas

- A aplicação suporta **ambos** SQL Server e PostgreSQL simultaneamente
- A detecção é automática baseada na connection string
- Não é necessário alterar código para alternar entre os dois

