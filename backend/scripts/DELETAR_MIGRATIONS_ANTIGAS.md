# Deletar Migrations Antigas (SQL Server)

Como estamos migrando para PostgreSQL e o banco está vazio, podemos deletar todas as migrations antigas criadas para SQL Server e manter apenas a nova `InitialPostgreSQL`.

## Migrations a Deletar

- 20251221131440_InitialCreate.cs e .Designer.cs
- 20251224140156_InitialDomainEntities.cs e .Designer.cs
- 20251224172508_AddPlansTable.cs e .Designer.cs
- 20251224231910_AddCreditPackages.cs e .Designer.cs
- 20251225012940_AddPaymentFieldsToNotesAndRelationships.cs e .Designer.cs
- 20251225013223_AddFileContentToMediaAssets.cs e .Designer.cs
- 20251225013630_RemovePaymentFieldsFromNotesAndRelationships.cs e .Designer.cs
- 20251225182455_AddCardScanResultRawText.cs e .Designer.cs
- 20251225212647_AddAiRawResponseToOcrExtract.cs e .Designer.cs
- 20251225213957_AddAgentConfiguration.cs e .Designer.cs

## Manter

- 20251225230450_InitialPostgreSQL.cs e .Designer.cs
- ApplicationDbContextModelSnapshot.cs (será regenerado)

## Após Deletar

Execute:

```bash
dotnet ef database update --project backend\src\AssistenteExecutivo.Infrastructure --startup-project backend\src\AssistenteExecutivo.Api
```

Isso aplicará apenas a migration `InitialPostgreSQL` que está configurada para PostgreSQL.


