# Cache de Sessões Distribuído

## Problema

Quando o serviço backend é reiniciado, todas as sessões de autenticação são perdidas, fazendo com que os usuários recebam erro 401 (Unauthorized) e precisem fazer login novamente.

## Causa

O sistema estava usando `AddDistributedMemoryCache()`, que armazena as sessões apenas em memória. Quando o serviço reinicia, toda a memória é limpa e as sessões são perdidas.

## Solução

Foi implementado cache distribuído usando SQL Server (`AddDistributedSqlServerCache()`), que armazena as sessões no banco de dados. Isso permite que as sessões persistam entre reinicializações do serviço.

## Configuração

### 1. Executar o Script SQL

Antes de iniciar a aplicação, execute o script `CreateSessionCacheTable.sql` no banco de dados:

```sql
-- Execute o script CreateSessionCacheTable.sql
```

Este script cria a tabela `SessionCache` no banco de dados, que será usada para armazenar as sessões.

### 2. Configuração Automática

A aplicação está configurada para usar automaticamente a connection string `DefaultConnection` do `appsettings.json`. A tabela será criada automaticamente na primeira execução se o usuário do banco tiver permissões adequadas, mas é recomendado executar o script manualmente.

### 3. Estrutura da Tabela

A tabela `SessionCache` contém:
- `Id`: Chave única da sessão (NVARCHAR(449))
- `Value`: Dados da sessão serializados (VARBINARY(MAX))
- `ExpiresAtTime`: Data/hora de expiração (DATETIMEOFFSET)
- `SlidingExpirationInSeconds`: Tempo de expiração deslizante em segundos (BIGINT, nullable)
- `AbsoluteExpiration`: Data/hora de expiração absoluta (DATETIMEOFFSET, nullable)

## Benefícios

1. **Persistência**: Sessões sobrevivem a reinicializações do serviço
2. **Escalabilidade**: Múltiplas instâncias do serviço podem compartilhar o mesmo cache
3. **Confiabilidade**: Não há perda de sessões em caso de falha do serviço

## Limpeza Automática

As entradas expiradas são automaticamente removidas pelo SQL Server distributed cache. Você pode também executar manualmente:

```sql
DELETE FROM [dbo].[SessionCache] WHERE [ExpiresAtTime] < SYSDATETIMEOFFSET();
```

## Notas

- O timeout de sessão continua sendo de 30 minutos (configurado em `Program.cs`)
- O cache tem um sliding expiration padrão de 20 minutos
- As sessões são armazenadas de forma segura no banco de dados



