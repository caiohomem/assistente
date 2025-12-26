# Análise de Estrutura: Clean Code e DDD

## 📋 Resumo Executivo

Esta análise avalia a estrutura do código entre as camadas **Controllers**, **Handlers**, **Domain Services** e **Domain** conforme boas práticas de **Clean Code** e **Domain-Driven Design (DDD)**.

### 🎯 Status Geral

| Aspecto | Status | Observações |
|--------|--------|-------------|
| **Separação de Camadas** | ⚠️ **Parcial** | Alguns controllers acessam diretamente o DbContext |
| **CQRS/MediatR** | ✅ **Bom** | Uso correto de Commands/Queries e MediatR |
| **Domain Entities** | ✅ **Bom** | Entidades ricas com encapsulamento |
| **Domain Services** | ⚠️ **Limitado** | Poucos domain services, lógica em handlers |
| **Validações** | ⚠️ **Misto** | Validações em controllers e handlers |

---

## ✅ Pontos Positivos

### 1. **Uso Correto de CQRS/MediatR**
- Controllers delegam para handlers via MediatR
- Separação clara entre Commands e Queries
- Handlers implementam `IRequestHandler<TRequest, TResponse>`

**Exemplo:**
```12:58:backend/src/AssistenteExecutivo.Api/Controllers/ContactsController.cs
public async Task<IActionResult> ListContacts(...)
{
    var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_db, cancellationToken);
    var query = new ListContactsQuery { ... };
    var result = await _mediator.Send(query, cancellationToken);
    return Ok(result);
}
```

### 2. **Entidades de Domínio Ricas**
- Entidades encapsulam lógica de negócio
- Uso de Value Objects (PersonName, EmailAddress, etc.)
- Factory methods estáticos (Contact.Create, Note.CreateTextNote)
- Domain Events implementados

**Exemplo:**
```60:69:backend/src/AssistenteExecutivo.Domain/Entities/Contact.cs
public static Contact Create(
    Guid contactId,
    Guid ownerUserId,
    PersonName name,
    IClock clock)
{
    var contact = new Contact(contactId, ownerUserId, name, clock);
    contact._domainEvents.Add(new ContactCreated(contactId, ownerUserId, "Manual", clock.UtcNow));
    return contact;
}
```

### 3. **Value Objects**
- Uso correto de Value Objects para encapsular validações
- Imutabilidade e comparação por valor

---

## ⚠️ Problemas Identificados

### 🔴 **CRÍTICO: Controllers Acessando DbContext Diretamente**

**Problema**: Controllers estão injetando e usando `ApplicationDbContext` diretamente, violando a separação de camadas.

**Arquivos Afetados:**
- `AuthController.cs` - Linhas 20, 151-152, 317-318, 378-379
- `ContactsController.cs` - Linha 21, 46, 72, 97, 126, 162, 202, 238, 275, 312, 349
- `NotesController.cs` - Linha 20, 44, 67, 97, 133
- `CreditsController.cs` - Linha 20, 137-172
- `CaptureController.cs` - Linha 20

**Exemplo Problemático:**
```143:195:backend/src/AssistenteExecutivo.Api/Controllers/AuthController.cs
// Verificar se o usuário existe no banco de dados
if (string.IsNullOrWhiteSpace(userInfo.Email))
{
    _logger.LogWarning("Email vazio retornado do Keycloak. UserInfo: Sub={Sub}, Name={Name}", userInfo.Sub, userInfo.Name);
    return Redirect(BuildFrontendRedirectUrl(appendQuery: "authError=email_nao_disponivel"));
}

var normalizedEmail = EmailAddress.Create(userInfo.Email).Value;
var existingUser = await _db.UserProfiles
    .FirstOrDefaultAsync(u => u.Email.Value == normalizedEmail, HttpContext.RequestAborted);

// Se usuário não existe, criar automaticamente usando dados do Keycloak
if (existingUser == null)
{
    _logger.LogInformation("Usuário {Email} não encontrado no banco. Criando UserProfile automaticamente com dados do Keycloak.", userInfo.Email);
    
    try
    {
        // Extrair primeiro nome e sobrenome do userInfo
        var firstName = userInfo.GivenName ?? userInfo.Name?.Split(' ').FirstOrDefault() ?? "Usuário";
        var lastName = userInfo.FamilyName ?? userInfo.Name?.Split(' ').Skip(1).FirstOrDefault() ?? string.Empty;

        // Criar UserProfile automaticamente
        var userId = Guid.NewGuid();
        var keycloakSubject = KeycloakSubject.Create(userInfo.Sub);
        var email = EmailAddress.Create(userInfo.Email);
        var displayName = PersonName.Create(firstName, lastName);

        var userProfile = new UserProfile(
            userId: userId,
            keycloakSubject: keycloakSubject,
            email: email,
            displayName: displayName,
            clock: _clock);

        _db.UserProfiles.Add(userProfile);
        await _db.SaveChangesAsync(HttpContext.RequestAborted);
```

**Impacto:**
- Violação do princípio de separação de responsabilidades
- Lógica de negócio no controller
- Dificulta testes unitários
- Acoplamento com EF Core na camada de apresentação

**Solução Recomendada:**
1. Criar um Command/Query para obter UserId do contexto
2. Mover lógica de criação de UserProfile para um handler
3. Usar apenas MediatR nos controllers

---

### 🟡 **MÉDIO: Validações em Múltiplas Camadas**

**Problema**: Validações estão sendo feitas tanto em controllers quanto em handlers, causando duplicação.

**Exemplo 1 - Controller:**
```54:68:backend/src/AssistenteExecutivo.Api/Controllers/AuthController.cs
if (command == null)
    return BadRequest(new { message = "Requisição inválida." });

if (string.IsNullOrWhiteSpace(command.Email))
    return BadRequest(new { message = "Email é obrigatório." });

if (string.IsNullOrWhiteSpace(command.Password))
    return BadRequest(new { message = "Senha é obrigatória." });

if (command.Password.Length < 8)
    return BadRequest(new { message = "Senha deve ter no mínimo 8 caracteres." });

if (string.IsNullOrWhiteSpace(command.FirstName))
    return BadRequest(new { message = "Primeiro nome é obrigatório." });
```

**Exemplo 2 - Handler:**
```28:42:backend/src/AssistenteExecutivo.Application/Handlers/Credits/ConsumeCreditsCommandHandler.cs
// Validar OwnerUserId
if (request.OwnerUserId == Guid.Empty)
    throw new ArgumentException("OwnerUserId é obrigatório", nameof(request.OwnerUserId));

// Validar Amount
if (request.Amount <= 0)
    throw new ArgumentException("Amount deve ser maior que zero", nameof(request.Amount));

// Validar IdempotencyKey
if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
    throw new ArgumentException("IdempotencyKey é obrigatório", nameof(request.IdempotencyKey));

// Validar Purpose
if (string.IsNullOrWhiteSpace(request.Purpose))
    throw new ArgumentException("Purpose é obrigatório", nameof(request.Purpose));
```

**Impacto:**
- Duplicação de código
- Inconsistência entre validações
- Manutenção difícil

**Solução Recomendada:**
1. **Validações de entrada (formato)**: Usar FluentValidation nos Commands/Queries
2. **Validações de negócio**: Mover para Domain Entities ou Domain Services
3. **Controllers**: Apenas validação básica de null/ModelState

---

### 🟡 **MÉDIO: Lógica de Negócio em Handlers**

**Problema**: Alguns handlers contêm lógica de negócio que deveria estar em Domain Services.

**Exemplo:**
```35:96:backend/src/AssistenteExecutivo.Application/Handlers/Auth/RegisterUserCommandHandler.cs
public async Task<RegisterUserResult> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
{
    // Validar email único
    var normalizedEmail = EmailAddress.Create(request.Email).Value;
    var existingUser = await _db.UserProfiles
        .FirstOrDefaultAsync(u => u.Email.Value == normalizedEmail, cancellationToken);

    if (existingUser != null)
    {
        throw new InvalidOperationException("Este email já está cadastrado. Faça login em vez de criar uma nova conta.");
    }

    // Obter realm do Keycloak
    var realmId = _configuration["Keycloak:Realm"] ?? "assistenteexecutivo";

    // Verificar se usuário já existe no Keycloak
    var keycloakUserId = await _keycloakService.GetUserIdByEmailAsync(realmId, request.Email, cancellationToken);

    if (string.IsNullOrEmpty(keycloakUserId))
    {
        // Criar usuário no Keycloak
        keycloakUserId = await _keycloakService.CreateUserAsync(
            realmId,
            request.Email,
            request.FirstName,
            request.LastName,
            request.Password,
            cancellationToken);

        _logger.LogInformation("Usuário {Email} criado no Keycloak com ID {UserId}", request.Email, keycloakUserId);
    }
    else
    {
        _logger.LogInformation("Usuário {Email} já existe no Keycloak com ID {UserId}", request.Email, keycloakUserId);
    }

    // Criar UserProfile no banco de dados
    var userId = Guid.NewGuid();
    var keycloakSubject = KeycloakSubject.Create(keycloakUserId);
    var email = EmailAddress.Create(request.Email);
    var displayName = PersonName.Create(request.FirstName, request.LastName);

    var userProfile = new UserProfile(
        userId: userId,
        keycloakSubject: keycloakSubject,
        email: email,
        displayName: displayName,
        clock: _clock);

    _db.UserProfiles.Add(userProfile);
    await _db.SaveChangesAsync(cancellationToken);
```

**Problemas:**
- Handler orquestra múltiplas operações (Keycloak + Database)
- Lógica de negócio complexa no handler
- Dificulta reutilização

**Solução Recomendada:**
1. Criar `UserRegistrationService` no Domain
2. Service orquestra: validação, Keycloak, criação de UserProfile
3. Handler apenas chama o service

---

### 🟡 **MÉDIO: Inconsistência no Tratamento de Exceções**

**Problema**: Diferentes tipos de exceções sendo usados em diferentes camadas.

**Exemplos:**
- Controllers: `InvalidOperationException`, `ArgumentException`
- Handlers: `ArgumentException`, `DomainException`
- Domain: `DomainException`

**Impacto:**
- Dificulta tratamento consistente de erros
- Middleware de exceções precisa tratar múltiplos tipos

**Solução Recomendada:**
1. Usar apenas `DomainException` para erros de negócio
2. Criar exceções específicas do domínio (ex: `ContactNotFoundException`)
3. Middleware centralizado para mapear exceções em HTTP status codes

---

### 🟢 **BAIXO: Falta de Domain Services**

**Problema**: Poucos Domain Services implementados. Apenas `ContactDeduplicationService` foi encontrado.

**Oportunidades:**
- `UserRegistrationService` - Orquestrar registro de usuário
- `ContactMergeService` - Lógica de merge de contatos
- `CreditTransactionService` - Regras complexas de transações

**Exemplo Positivo:**
```7:76:backend/src/AssistenteExecutivo.Domain/DomainServices/ContactDeduplicationService.cs
public class ContactDeduplicationService
{
    public DeduplicationDecision Decide(
        Contact existingContact,
        OcrExtract newExtract)
    {
        // Heurísticas de deduplicação
        // ...
    }
}
```

---

## 📊 Análise por Camada

### **Camada API (Controllers)**

| Aspecto | Status | Observações |
|---------|--------|-------------|
| **Responsabilidade Única** | ⚠️ | Alguns controllers têm lógica de negócio |
| **Uso de MediatR** | ✅ | Maioria usa corretamente |
| **Acesso a DbContext** | 🔴 | **CRÍTICO**: Múltiplos controllers acessam diretamente |
| **Validações** | ⚠️ | Validações duplicadas |

**Recomendações:**
1. Remover todas as dependências de `ApplicationDbContext` dos controllers
2. Usar apenas `IMediator` e `ILogger`
3. Mover validações para FluentValidation
4. Criar extension methods para obter `OwnerUserId` via MediatR

---

### **Camada Application (Handlers)**

| Aspecto | Status | Observações |
|---------|--------|-------------|
| **CQRS** | ✅ | Separação clara Commands/Queries |
| **Orquestração** | ⚠️ | Alguns handlers orquestram demais |
| **Validações** | ⚠️ | Validações de negócio em handlers |
| **Domain Services** | ⚠️ | Pouco uso de domain services |

**Recomendações:**
1. Handlers devem ser "thin" - apenas orquestrar
2. Mover lógica complexa para Domain Services
3. Usar FluentValidation para validações de entrada
4. Handlers devem apenas: validar entrada → chamar domain → persistir → publicar eventos

---

### **Camada Domain**

| Aspecto | Status | Observações |
|---------|--------|-------------|
| **Entidades Ricas** | ✅ | Boa encapsulamento |
| **Value Objects** | ✅ | Uso correto |
| **Domain Events** | ✅ | Implementado corretamente |
| **Domain Services** | ⚠️ | Poucos services |
| **Repositories (Interfaces)** | ✅ | Interfaces no Domain |

**Recomendações:**
1. Criar mais Domain Services para lógica complexa
2. Mover validações de negócio das entidades para services quando necessário
3. Considerar Aggregates para agrupar entidades relacionadas

---

## 🎯 Plano de Ação Recomendado

### **Prioridade ALTA**

1. **Remover DbContext dos Controllers**
   - Criar Queries para obter `OwnerUserId`
   - Mover lógica de criação de UserProfile para handler
   - Refatorar `AuthController.OAuthCallback`

2. **Implementar FluentValidation**
   - Adicionar validações nos Commands/Queries
   - Remover validações duplicadas dos controllers

3. **Criar Domain Services**
   - `UserRegistrationService`
   - `ContactMergeService` (se necessário)

### **Prioridade MÉDIA**

4. **Padronizar Exceções**
   - Usar apenas `DomainException` para erros de negócio
   - Criar exceções específicas (ex: `ContactNotFoundException`)
   - Middleware centralizado

5. **Refatorar Handlers Complexos**
   - `RegisterUserCommandHandler` → usar `UserRegistrationService`
   - Handlers devem ser "thin"

### **Prioridade BAIXA**

6. **Melhorar Documentação**
   - XML comments em todos os métodos públicos
   - Documentar regras de negócio

7. **Testes**
   - Unit tests para Domain Services
   - Integration tests para handlers

---

## 📝 Exemplos de Refatoração

### **Exemplo 1: Remover DbContext do Controller**

**ANTES:**
```csharp
public class ContactsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    
    public async Task<IActionResult> ListContacts(...)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_db, cancellationToken);
        // ...
    }
}
```

**DEPOIS:**
```csharp
public class ContactsController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public async Task<IActionResult> ListContacts(...)
    {
        var ownerUserId = await _mediator.Send(new GetOwnerUserIdQuery(), cancellationToken);
        // ...
    }
}
```

### **Exemplo 2: Mover Lógica para Domain Service**

**ANTES (Handler):**
```csharp
public async Task<RegisterUserResult> Handle(...)
{
    // Validação
    var existingUser = await _db.UserProfiles...
    
    // Keycloak
    var keycloakUserId = await _keycloakService...
    
    // Criar UserProfile
    var userProfile = new UserProfile(...);
    _db.UserProfiles.Add(userProfile);
}
```

**DEPOIS (Domain Service + Handler):**
```csharp
// Domain Service
public class UserRegistrationService
{
    public async Task<UserProfile> RegisterUserAsync(...)
    {
        // Toda a lógica aqui
    }
}

// Handler
public async Task<RegisterUserResult> Handle(...)
{
    var userProfile = await _userRegistrationService.RegisterUserAsync(...);
    await _unitOfWork.SaveChangesAsync(...);
    return new RegisterUserResult { ... };
}
```

---

## ✅ Conclusão

O código está **bem estruturado** em geral, com boa separação CQRS e entidades de domínio ricas. Os principais problemas são:

1. **Controllers acessando DbContext diretamente** (CRÍTICO)
2. **Validações duplicadas** entre camadas
3. **Lógica de negócio em handlers** que deveria estar em Domain Services

Com as refatorações sugeridas, o código estará alinhado com as melhores práticas de Clean Code e DDD.



