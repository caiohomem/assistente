# Scripts de Inicialização do Banco de Dados

## Script Completo de Inicialização

### `InitializeDatabase.postgresql.sql`

Script único que executa todos os passos de inicialização:
- Insere dados iniciais
- Insere dados iniciais (AgentConfiguration, CreditPackages)

**Como usar:**

```bash
# 1. Aplicar migrations do EF Core primeiro
cd backend/src/AssistenteExecutivo.Api
dotnet ef database update

# 2. Executar script de inicialização
psql -h seu-host -U seu-usuario -d seu-banco -f ../../scripts/InitializeDatabase.postgresql.sql
```

## Scripts Individuais

### Limpeza do Banco

- **`DropAllTablesSimple.postgresql.sql`** - Remove todas as tabelas (método rápido)
- **`DropAllTables.postgresql.sql`** - Remove todas as tabelas (método detalhado com logs)

### Dados Iniciais

- **`InsertAgentConfigurationPrompt.postgresql.sql`** - Insere prompts padrão do agente
- **`InsertCreditPackages.postgresql.sql`** - Insere packages de créditos padrão
- **`InsertEmailTemplates.postgresql.sql`** - Insere templates de email do sistema (boas-vindas, recuperação de senha, etc.)

### Notas

- **SessionCache**: Não é mais necessário - usamos Redis para cache de sessão
- **Logs**: Não é mais necessário - usamos Google Cloud Console para logs em produção

## Ordem Recomendada de Execução

### Para banco novo ou limpo:

1. **Aplicar migrations:**
   ```bash
   cd backend/src/AssistenteExecutivo.Api
   dotnet ef database update
   ```

2. **Executar script completo:**
   ```bash
   psql -h seu-host -U seu-usuario -d seu-banco -f backend/scripts/InitializeDatabase.postgresql.sql
   ```

3. **Inserir templates de email (opcional, já incluído no DatabaseSeeder):**
   ```bash
   psql -h seu-host -U seu-usuario -d seu-banco -f backend/scripts/InsertEmailTemplates.postgresql.sql
   ```
   
   **Nota:** Os templates de email também são criados automaticamente pelo `DatabaseSeeder` quando a aplicação inicia. O script SQL é útil para atualizar templates manualmente ou em ambientes onde o seeder não é executado.

### Para limpar e recriar tudo:

1. **Limpar banco:**
   ```bash
   psql -h seu-host -U seu-usuario -d seu-banco -f backend/scripts/DropAllTablesSimple.postgresql.sql
   ```

2. **Aplicar migrations:**
   ```bash
   cd backend/src/AssistenteExecutivo.Api
   dotnet ef database update
   ```

3. **Executar script completo:**
   ```bash
   psql -h seu-host -U seu-usuario -d seu-banco -f backend/scripts/InitializeDatabase.postgresql.sql
   ```

## Notas Importantes

- ⚠️ **ATENÇÃO**: Os scripts de limpeza (`DropAllTables*.sql`) removem TODAS as tabelas e dados!
- ✅ Os scripts de inserção verificam se os dados já existem antes de inserir (idempotentes)
- ✅ As migrations do EF Core criam todas as tabelas principais do domínio
- ✅ Os scripts SQL criam apenas tabelas auxiliares e inserem dados iniciais

