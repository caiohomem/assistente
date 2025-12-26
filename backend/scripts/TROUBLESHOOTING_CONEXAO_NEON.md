# Troubleshooting de Conexão com Neon PostgreSQL

## Situação Atual

Se "Allow traffic via the public internet" está **ON** no console do Neon, então **não há restrição de IP**. O problema pode ser outro.

## Possíveis Causas e Soluções

### 1. Formato da Connection String

Verifique se está usando o formato correto:

**✅ Formato Correto (Parâmetros):**
```
Host=ep-orange-math-abkd6qp0-pooler.eu-west-2.aws.neon.tech;Database=assistente;Username=neondb_owner;Password=sua-senha;SSL Mode=Require;
```

**✅ Formato Correto (URL):**
```
postgresql://neondb_owner:senha@ep-orange-math-abkd6qp0-pooler.eu-west-2.aws.neon.tech/assistente?sslmode=require
```

**❌ Formato Incorreto (misturado):**
```
Host=postgresql://user:pass@host/db;Database=assistente;
```

### 2. Verificar Credenciais

- Confirme que o **Username** está correto
- Confirme que o **Password** está correto (sem espaços extras)
- Confirme que o **Database** existe e está correto

### 3. Testar Conexão Direta

#### Opção A: Usando psql (se tiver instalado)
```bash
psql "Host=ep-orange-math-abkd6qp0-pooler.eu-west-2.aws.neon.tech;Database=assistente;Username=neondb_owner;Password=sua-senha;SSL Mode=Require;"
```

#### Opção B: Usando pgAdmin ou DBeaver
- Crie uma nova conexão
- Use os mesmos parâmetros da connection string
- Teste a conexão

#### Opção C: Teste via código C#
Crie um pequeno script de teste:

```csharp
using Npgsql;

var connString = "Host=ep-orange-math-abkd6qp0-pooler.eu-west-2.aws.neon.tech;Database=assistente;Username=neondb_owner;Password=sua-senha;SSL Mode=Require;";

try
{
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    Console.WriteLine("✅ Conexão bem-sucedida!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Erro: {ex.Message}");
}
```

### 4. Problemas de DNS/Resolução

O ping pode falhar mesmo com conexão funcionando (alguns servidores bloqueiam ICMP).

**Teste alternativo:**
```bash
nslookup ep-orange-math-abkd6qp0-pooler.eu-west-2.aws.neon.tech
```

### 5. Firewall Local/Antivírus

- Verifique se seu firewall não está bloqueando conexões PostgreSQL (porta 5432)
- Verifique se seu antivírus não está bloqueando
- Tente desabilitar temporariamente para testar

### 6. SSL/TLS

Certifique-se de que `SSL Mode=Require` está na connection string.

**Opções de SSL Mode:**
- `Require` - Obrigatório (recomendado para Neon)
- `Prefer` - Preferencial
- `Disable` - Desabilitado (não recomendado)

### 7. Pooler vs Direct Connection

O Neon oferece dois tipos:

**Pooler (recomendado):**
```
Host=ep-xxx-xxx-pooler.region.aws.neon.tech
```

**Direct:**
```
Host=ep-xxx-xxx.region.aws.neon.tech
```

Certifique-se de usar o **pooler** (com `-pooler` no hostname).

### 8. Verificar Logs da Aplicação

Execute a aplicação e verifique os logs para ver o erro exato:

```bash
dotnet run --project backend/src/AssistenteExecutivo.Api
```

Procure por mensagens como:
- "No such host is known"
- "Connection refused"
- "Timeout"
- "SSL/TLS error"

### 9. Testar com Connection String do Console Neon

1. No console do Neon, vá em **"Connection Details"**
2. Copie a connection string **exata** fornecida
3. Use ela diretamente no `appsettings.json`

### 10. Verificar Porta

O Neon usa a porta padrão do PostgreSQL (5432). Se sua connection string não especifica porta, ela usa a padrão.

Se necessário, adicione explicitamente:
```
Host=...;Port=5432;Database=...;...
```

## Checklist Rápido

- [ ] Connection string no formato correto
- [ ] Credenciais corretas (username, password, database)
- [ ] SSL Mode=Require está presente
- [ ] Usando hostname com `-pooler`
- [ ] Firewall local não está bloqueando
- [ ] Testou conexão direta com psql/pgAdmin
- [ ] Verificou logs da aplicação para erro específico

## Próximos Passos

1. **Teste a connection string diretamente** com psql ou pgAdmin
2. **Verifique os logs da aplicação** para ver o erro exato
3. **Compare sua connection string** com a fornecida no console do Neon

Se ainda não funcionar, compartilhe:
- O erro exato dos logs
- A connection string (sem a senha, claro)
- Resultado do teste com psql/pgAdmin

