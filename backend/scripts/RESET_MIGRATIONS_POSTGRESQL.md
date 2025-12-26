# Reset Migrations para PostgreSQL

O problema é que o snapshot do modelo ainda está configurado para SQL Server, causando conflito ao criar migrations para PostgreSQL.

## Solução: Deletar Snapshot e Criar Nova Migration Inicial

### Passo 1: Deletar o Snapshot

Delete o arquivo:
```
backend/src/AssistenteExecutivo.Infrastructure/Migrations/ApplicationDbContextModelSnapshot.cs
```

### Passo 2: Deletar a Tabela de Migrations no Banco (se já aplicou)

Se você já aplicou migrations no PostgreSQL, delete a tabela `__EFMigrationsHistory`:

```sql
DROP TABLE IF EXISTS "__EFMigrationsHistory";
```

### Passo 3: Criar Nova Migration Inicial

```bash
dotnet ef migrations add InitialPostgreSQL --project backend/src/AssistenteExecutivo.Infrastructure --startup-project backend/src/AssistenteExecutivo.Api
```

### Passo 4: Aplicar a Migration

```bash
dotnet ef database update --project backend/src/AssistenteExecutivo.Infrastructure --startup-project backend/src/AssistenteExecutivo.Api
```

## Alternativa: Manter Migrations Existentes

Se preferir manter as migrations existentes, você pode:

1. Aplicar as migrations SQL Server diretamente no PostgreSQL (o EF Core traduz automaticamente)
2. Ou editar manualmente as migrations para usar sintaxe PostgreSQL

## Nota

As migrations SQL Server geralmente funcionam no PostgreSQL com pequenos ajustes, mas o snapshot precisa ser regenerado para PostgreSQL.

