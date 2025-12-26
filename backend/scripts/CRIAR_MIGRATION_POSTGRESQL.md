# Criar Nova Migration para PostgreSQL

Após corrigir as configurações do EF Core, você precisa criar uma nova migration para sincronizar o modelo com o banco de dados.

## Comando

Execute no Package Manager Console ou terminal:

```bash
dotnet ef migrations add FixPostgreSQLModel --project backend/src/AssistenteExecutivo.Infrastructure --startup-project backend/src/AssistenteExecutivo.Api
```

## O que foi corrigido

1. **ValueComparer para `_features`**: Adicionado comparador de valores para a coleção de features
2. **PlanLimits**: Configurado como opcional mas sempre criado
3. **Address**: Configurado para evitar problemas com optional dependent

## Após criar a migration

Execute para aplicar:

```bash
dotnet ef database update --project backend/src/AssistenteExecutivo.Infrastructure --startup-project backend/src/AssistenteExecutivo.Api
```

Ou simplesmente execute a aplicação - ela aplicará as migrations automaticamente.

