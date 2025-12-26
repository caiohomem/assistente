# Como Obter a Connection String Correta do Neon

## Passo a Passo

### 1. Acesse o Console do Neon
- Vá para: https://console.neon.tech
- Faça login e selecione seu projeto

### 2. Encontre "Connection Details"
- No painel do projeto, procure por **"Connection Details"** ou **"Connection String"**
- Pode estar em:
  - Dashboard principal
  - Aba "Settings"
  - Menu lateral

### 3. Copie a Connection String
O Neon geralmente oferece duas opções:

**Opção A: Connection String (formato de parâmetros)**
```
Host=ep-xxx-xxx-pooler.region.aws.neon.tech;Database=neondb;Username=usuario;Password=senha;SSL Mode=Require;
```

**Opção B: Connection URI (formato de URL)**
```
postgresql://usuario:senha@ep-xxx-xxx-pooler.region.aws.neon.tech/neondb?sslmode=require
```

### 4. Use no appsettings.json

Cole a connection string **exata** que o Neon forneceu no campo `DefaultConnection`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Cole aqui a connection string do Neon"
  }
}
```

## Verificações Importantes

### ✅ Certifique-se de que:
- O **Database** está correto (pode ser `neondb`, `assistente`, ou outro nome)
- O **Username** está correto
- O **Password** está correto (sem espaços extras no início/fim)
- O hostname contém `-pooler` (recomendado)
- `SSL Mode=Require` está presente

### ❌ Evite:
- Modificar a connection string fornecida pelo Neon
- Adicionar parâmetros desnecessários
- Misturar formato de URL com formato de parâmetros

## Exemplo de Connection String do Neon

**Formato de Parâmetros (Recomendado):**
```
Host=ep-orange-math-abkd6qp0-pooler.eu-west-2.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=abc123xyz;SSL Mode=Require;
```

**Formato de URL (Também funciona):**
```
postgresql://neondb_owner:abc123xyz@ep-orange-math-abkd6qp0-pooler.eu-west-2.aws.neon.tech/neondb?sslmode=require
```

## Teste Rápido

Após copiar a connection string do Neon:

1. Cole no `appsettings.Development.json`
2. Execute a aplicação: `dotnet run`
3. Verifique se conecta sem erros

Se ainda houver erro, verifique:
- Se o banco de dados existe
- Se as credenciais estão corretas
- Se o hostname está correto

