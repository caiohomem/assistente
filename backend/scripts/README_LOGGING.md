# Configuração de Logging com Serilog

Este documento descreve a configuração de logging implementada no projeto AssistenteExecutivo.Api usando Serilog com sink para SQL Server.

## Componentes Implementados

### 1. Middleware de Tratamento de Exceções Global
- **Arquivo**: `Middleware/GlobalExceptionHandlingMiddleware.cs`
- **Funcionalidades**:
  - Captura todas as exceções (DomainException e exceções genéricas)
  - Registra logs detalhados usando Serilog
  - Retorna mensagens amigáveis para o cliente
  - Em produção, oculta detalhes técnicos das exceções
  - Em desenvolvimento, mostra detalhes completos para debug

### 2. Configuração do Serilog
- **Arquivo**: `Program.cs`
- **Sinks configurados**:
  - **Console**: Para logs durante desenvolvimento
  - **SQL Server**: Para persistência em produção
- **Níveis de log**:
  - Information: Logs gerais da aplicação
  - Warning: Exceções de domínio (erros de negócio esperados)
  - Error: Exceções não tratadas (erros inesperados)
  - Fatal: Erros críticos que encerram a aplicação

### 3. Tabela de Logs no SQL Server
- **Script**: `scripts/CreateLogsTable.sql`
- **Tabela**: `dbo.Logs`
- **Colunas principais**:
  - `Id`: Identificador único
  - `Message`: Mensagem do log
  - `Level`: Nível do log (Information, Warning, Error, Fatal)
  - `TimeStamp`: Data e hora do log
  - `Exception`: Stack trace completo da exceção
  - `SourceContext`: Contexto de origem (classe/método)
  - `RequestPath`: Caminho da requisição HTTP
  - `RequestMethod`: Método HTTP (GET, POST, etc.)
  - `StatusCode`: Código de status HTTP
  - `Elapsed`: Tempo de processamento em milissegundos
  - `UserName`: Nome do usuário autenticado
  - `MachineName`: Nome da máquina
  - `Environment`: Ambiente (Development, Production, etc.)

## Instalação

### 1. Executar o Script SQL
Antes de iniciar a aplicação, execute o script SQL para criar a tabela de logs:

```sql
-- Execute o script em: backend/scripts/CreateLogsTable.sql
```

### 2. Configurar Connection String
Certifique-se de que a connection string `DefaultConnection` está configurada no `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=seu-servidor;Database=AssistenteExecutivo;..."
  }
}
```

### 3. Instalar Pacotes NuGet
Os seguintes pacotes já foram adicionados ao projeto:
- `Serilog.AspNetCore` (v8.0.3)
- `Serilog.Sinks.MSSqlServer` (v6.6.1)
- `Serilog.Sinks.Console` (v6.0.0)

## Uso

### Tratamento Automático de Exceções
O middleware captura automaticamente todas as exceções. Não é necessário fazer nada especial nos controllers.

### Exceções de Domínio
Para exceções de domínio, use a classe `DomainException`:

```csharp
throw new DomainException("Domain:EmailJaExiste");
```

O middleware irá:
1. Registrar um log de Warning
2. Retornar uma mensagem localizada para o cliente
3. Retornar status HTTP 400 (Bad Request)

### Exceções Genéricas
Para outras exceções não tratadas:

```csharp
throw new InvalidOperationException("Algo deu errado");
```

O middleware irá:
1. Registrar um log de Error com detalhes completos
2. Retornar uma mensagem amigável em produção
3. Retornar detalhes completos em desenvolvimento
4. Retornar status HTTP 500 (Internal Server Error)

## Mensagens de Erro

As mensagens de erro são configuradas em `Resources/Messages.pt-BR.json`:

```json
{
  "Errors": {
    "InternalServerError": "Ocorreu um erro inesperado...",
    "BadRequest": "A requisição contém dados inválidos...",
    ...
  }
}
```

## Consultas Úteis

### Consultar logs de erro
```sql
SELECT * FROM Logs 
WHERE Level = 'Error' 
ORDER BY TimeStamp DESC
```

### Consultar logs de uma requisição específica
```sql
SELECT * FROM Logs 
WHERE RequestPath = '/api/contacts' 
  AND RequestMethod = 'POST'
ORDER BY TimeStamp DESC
```

### Consultar logs de um usuário
```sql
SELECT * FROM Logs 
WHERE UserName = 'usuario@example.com'
ORDER BY TimeStamp DESC
```

### Consultar requisições lentas
```sql
SELECT * FROM Logs 
WHERE Elapsed > 1000  -- Mais de 1 segundo
ORDER BY Elapsed DESC
```

## Manutenção

### Limpar logs antigos
Execute periodicamente para manter o banco de dados limpo:

```sql
-- Manter apenas os últimos 30 dias
DELETE FROM Logs 
WHERE TimeStamp < DATEADD(DAY, -30, GETDATE())
```

### Índices
A tabela já possui índices otimizados para:
- `TimeStamp` (ordem decrescente)
- `Level`
- `SourceContext`
- `RequestPath`

## Observações

- Apenas logs de nível Warning ou superior são salvos no SQL Server para reduzir o volume de dados
- Logs de Information são exibidos apenas no console
- Em produção, detalhes técnicos das exceções são ocultados do cliente por segurança
- O middleware deve ser registrado antes de `UseAuthentication()` e `UseAuthorization()` no pipeline

