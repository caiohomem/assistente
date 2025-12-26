# Formato de Connection String para PostgreSQL (Neon)

## Formatos Aceitos

O Npgsql aceita dois formatos de connection string:

### 1. Formato de Parâmetros (Recomendado)

```
Host=ep-orange-math-abkd6qp0-pooler.eu-west-2.aws.neon.tech;Database=assistente;Username=neondb_owner;Password=sua-senha;SSL Mode=Require;
```

**Parâmetros principais:**
- `Host`: Endereço do servidor (sem `postgresql://`)
- `Database`: Nome do banco de dados
- `Username`: Nome de usuário
- `Password`: Senha
- `SSL Mode`: `Require` (obrigatório para Neon)

### 2. Formato de URL

```
postgresql://username:password@host:port/database?sslmode=require
```

**Exemplo:**
```
postgresql://neondb_owner:senha@ep-orange-math-abkd6qp0-pooler.eu-west-2.aws.neon.tech/neondb?sslmode=require
```

## Como Obter a Connection String do Neon

1. Acesse o dashboard do Neon
2. Vá em "Connection Details" ou "Connection String"
3. Copie a connection string fornecida

**Importante:** Se o Neon fornecer uma URL (`postgresql://...`), você pode usar diretamente ou converter para o formato de parâmetros.

## Conversão de URL para Formato de Parâmetros

Se você receber uma URL como:
```
postgresql://user:pass@host.neon.tech/database?sslmode=require
```

Converta para:
```
Host=host.neon.tech;Database=database;Username=user;Password=pass;SSL Mode=Require;
```

## Erro Comum

❌ **ERRADO:**
```
Host=postgresql://user:pass@host.neon.tech/database;Database=assistente;
```

✅ **CORRETO:**
```
Host=host.neon.tech;Database=assistente;Username=user;Password=pass;SSL Mode=Require;
```

Ou simplesmente use a URL diretamente:
```
postgresql://user:pass@host.neon.tech/database?sslmode=require
```

## Configuração no appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=seu-host.neon.tech;Database=seu-database;Username=seu-usuario;Password=sua-senha;SSL Mode=Require;"
  }
}
```

