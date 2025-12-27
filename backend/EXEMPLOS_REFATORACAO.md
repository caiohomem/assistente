# Exemplos Práticos de Refatoração

Este documento apresenta exemplos concretos de como refatorar o código para seguir as melhores práticas de Clean Code e DDD.

---

## 🔴 Problema 1: Controller Acessando DbContext

### **Código Atual (PROBLEMÁTICO)**

**Arquivo:** `AuthController.cs` - Método `OAuthCallback`

```csharp
[HttpGet("oauth-callback")]
public async Task<IActionResult> OAuthCallback(...)
{
    // ❌ PROBLEMA: Controller acessando DbContext diretamente
    var normalizedEmail = EmailAddress.Create(userInfo.Email).Value;
    var existingUser = await _db.UserProfiles
        .FirstOrDefaultAsync(u => u.Email.Value == normalizedEmail, HttpContext.RequestAborted);

    // ❌ PROBLEMA: Lógica de negócio no controller
    if (existingUser == null)
    {
        // Criar UserProfile automaticamente
        var userId = Guid.NewGuid();
        var keycloakSubject = KeycloakSubject.Create(userInfo.Sub);
        var email = EmailAddress.Create(userInfo.Email);
        var displayName = PersonName.Create(firstName, lastName);

        var userProfile = new UserProfile(...);
        _db.UserProfiles.Add(userProfile);
        await _db.SaveChangesAsync(HttpContext.RequestAborted);
    }
}
```

### **Solução: Criar Command Handler**

**1. Criar Command:**
```csharp
// Application/Commands/Auth/ProvisionUserFromKeycloakCommand.cs
public record ProvisionUserFromKeycloakCommand : IRequest<ProvisionUserResult>
{
    public string KeycloakSubject { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? GivenName { get; init; }
    public string? FamilyName { get; init; }
    public string? Name { get; init; }
}

public record ProvisionUserResult
{
    public Guid UserId { get; init; }
    public bool WasCreated { get; init; }
}
```

**2. Criar Handler:**
```csharp
// Application/Handlers/Auth/ProvisionUserFromKeycloakCommandHandler.cs
public class ProvisionUserFromKeycloakCommandHandler 
    : IRequestHandler<ProvisionUserFromKeycloakCommand, ProvisionUserResult>
{
    private readonly IApplicationDbContext _db;
    private readonly IClock _clock;
    private readonly ILogger<ProvisionUserFromKeycloakCommandHandler> _logger;

    public async Task<ProvisionUserResult> Handle(
        ProvisionUserFromKeycloakCommand request, 
        CancellationToken cancellationToken)
    {
        var normalizedEmail = EmailAddress.Create(request.Email).Value;
        var existingUser = await _db.UserProfiles
            .FirstOrDefaultAsync(u => u.Email.Value == normalizedEmail, cancellationToken);

        if (existingUser != null)
        {
            return new ProvisionUserResult
            {
                UserId = existingUser.UserId,
                WasCreated = false
            };
        }

        // Extrair nome
        var firstName = request.GivenName 
            ?? request.Name?.Split(' ').FirstOrDefault() 
            ?? "Usuário";
        var lastName = request.FamilyName 
            ?? request.Name?.Split(' ').Skip(1).FirstOrDefault() 
            ?? string.Empty;

        // Criar UserProfile
        var userId = Guid.NewGuid();
        var keycloakSubject = KeycloakSubject.Create(request.KeycloakSubject);
        var email = EmailAddress.Create(request.Email);
        var displayName = PersonName.Create(firstName, lastName);

        var userProfile = new UserProfile(
            userId: userId,
            keycloakSubject: keycloakSubject,
            email: email,
            displayName: displayName,
            clock: _clock);

        _db.UserProfiles.Add(userProfile);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "UserProfile criado automaticamente para {Email} (UserId={UserId})",
            request.Email, userId);

        return new ProvisionUserResult
        {
            UserId = userId,
            WasCreated = true
        };
    }
}
```

**3. Refatorar Controller:**
```csharp
[HttpGet("oauth-callback")]
public async Task<IActionResult> OAuthCallback(...)
{
    // ✅ SOLUÇÃO: Usar MediatR
    var provisionCommand = new ProvisionUserFromKeycloakCommand
    {
        KeycloakSubject = userInfo.Sub,
        Email = userInfo.Email,
        GivenName = userInfo.GivenName,
        FamilyName = userInfo.FamilyName,
        Name = userInfo.Name
    };

    var provisionResult = await _mediator.Send(provisionCommand, HttpContext.RequestAborted);

    if (!provisionResult.WasCreated && string.IsNullOrWhiteSpace(userInfo.Email))
    {
        return Redirect(BuildFrontendRedirectUrl(appendQuery: "authError=email_nao_disponivel"));
    }

    // Resto da lógica de sessão...
}
```

---

## 🔴 Problema 2: Obter OwnerUserId no Controller

### **Código Atual (PROBLEMÁTICO)**

**Arquivo:** `ContactsController.cs`

```csharp
public async Task<IActionResult> ListContacts(...)
{
    // ❌ PROBLEMA: Controller acessando DbContext
    var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_db, cancellationToken);
    
    var query = new ListContactsQuery { OwnerUserId = ownerUserId, ... };
    var result = await _mediator.Send(query, cancellationToken);
    return Ok(result);
}
```

### **Solução: Criar Query**

**1. Criar Query:**
```csharp
// Application/Queries/Auth/GetOwnerUserIdQuery.cs
public record GetOwnerUserIdQuery : IRequest<Guid>
{
    public string? KeycloakSubject { get; init; }
}

// Application/Handlers/Auth/GetOwnerUserIdQueryHandler.cs
public class GetOwnerUserIdQueryHandler 
    : IRequestHandler<GetOwnerUserIdQuery, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ILogger<GetOwnerUserIdQueryHandler> _logger;

    public async Task<Guid> Handle(
        GetOwnerUserIdQuery request, 
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.KeycloakSubject))
        {
            throw new DomainException("Domain:KeycloakSubjectObrigatorio");
        }

        var keycloakSubject = KeycloakSubject.Create(request.KeycloakSubject);
        var userProfile = await _db.UserProfiles
            .FirstOrDefaultAsync(
                u => u.KeycloakSubject.Value == keycloakSubject.Value,
                cancellationToken);

        if (userProfile == null)
        {
            _logger.LogWarning(
                "UserProfile não encontrado para KeycloakSubject: {KeycloakSubject}",
                keycloakSubject.Value);
            throw new DomainException("Domain:UserProfileNaoEncontrado");
        }

        return userProfile.UserId;
    }
}
```

**2. Criar Extension Method:**
```csharp
// Api/Extensions/HttpContextExtensions.cs
public static class HttpContextExtensions
{
    public static async Task<Guid> GetRequiredOwnerUserIdAsync(
        this HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken = default)
    {
        var subClaim = httpContext.User.FindFirst("sub")?.Value
            ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(subClaim))
        {
            throw new UnauthorizedAccessException("Sub claim não encontrado no token");
        }

        var query = new GetOwnerUserIdQuery { KeycloakSubject = subClaim };
        return await mediator.Send(query, cancellationToken);
    }
}
```

**3. Refatorar Controller:**
```csharp
public async Task<IActionResult> ListContacts(...)
{
    // ✅ SOLUÇÃO: Usar MediatR via extension method
    var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);
    
    var query = new ListContactsQuery { OwnerUserId = ownerUserId, ... };
    var result = await _mediator.Send(query, cancellationToken);
    return Ok(result);
}
```

---

## 🟡 Problema 3: Validações Duplicadas

### **Código Atual (PROBLEMÁTICO)**

**Controller:**
```csharp
[HttpPost("register")]
public async Task<ActionResult<RegisterUserResult>> Register([FromBody] RegisterUserCommand command)
{
    // ❌ PROBLEMA: Validações no controller
    if (command == null)
        return BadRequest(new { message = "Requisição inválida." });
    
    if (string.IsNullOrWhiteSpace(command.Email))
        return BadRequest(new { message = "Email é obrigatório." });
    
    if (string.IsNullOrWhiteSpace(command.Password))
        return BadRequest(new { message = "Senha é obrigatória." });
    
    if (command.Password.Length < 8)
        return BadRequest(new { message = "Senha deve ter no mínimo 8 caracteres." });
    
    // ...
}
```

**Handler:**
```csharp
public async Task<RegisterUserResult> Handle(RegisterUserCommand request, ...)
{
    // ❌ PROBLEMA: Validações duplicadas no handler
    var normalizedEmail = EmailAddress.Create(request.Email).Value;
    // EmailAddress.Create já valida, mas não há validação de senha aqui
    // ...
}
```

### **Solução: Usar FluentValidation**

**1. Instalar FluentValidation:**
```bash
dotnet add package FluentValidation
dotnet add package FluentValidation.DependencyInjectionExtensions
```

**2. Criar Validator:**
```csharp
// Application/Commands/Auth/RegisterUserCommandValidator.cs
public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email é obrigatório")
            .EmailAddress()
            .WithMessage("Email inválido");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Senha é obrigatória")
            .MinimumLength(8)
            .WithMessage("Senha deve ter no mínimo 8 caracteres")
            .Matches("[A-Z]")
            .WithMessage("Senha deve conter pelo menos uma letra maiúscula")
            .Matches("[a-z]")
            .WithMessage("Senha deve conter pelo menos uma letra minúscula")
            .Matches("[0-9]")
            .WithMessage("Senha deve conter pelo menos um número");

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("Primeiro nome é obrigatório")
            .MaximumLength(100)
            .WithMessage("Primeiro nome deve ter no máximo 100 caracteres");

        RuleFor(x => x.LastName)
            .MaximumLength(100)
            .WithMessage("Sobrenome deve ter no máximo 100 caracteres");
    }
}
```

**3. Registrar no DI:**
```csharp
// Program.cs ou DependencyInjection.cs
services.AddValidatorsFromAssembly(typeof(RegisterUserCommandValidator).Assembly);
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
```

**4. Criar ValidationBehavior:**
```csharp
// Application/Behaviors/ValidationBehavior.cs
public class ValidationBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .Where(r => r.Errors.Any())
            .SelectMany(r => r.Errors)
            .ToList();

        if (failures.Any())
        {
            throw new ValidationException(failures);
        }

        return await next();
    }
}
```

**5. Refatorar Controller:**
```csharp
[HttpPost("register")]
public async Task<ActionResult<RegisterUserResult>> Register([FromBody] RegisterUserCommand command)
{
    // ✅ SOLUÇÃO: Validação automática via FluentValidation
    // Se chegar aqui, o comando já foi validado
    try
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }
    catch (ValidationException ex)
    {
        return BadRequest(new { errors = ex.Errors });
    }
}
```

---

## 🟡 Problema 4: Lógica de Negócio em Handler

### **Código Atual (PROBLEMÁTICO)**

**Handler:**
```csharp
public async Task<RegisterUserResult> Handle(RegisterUserCommand request, ...)
{
    // ❌ PROBLEMA: Handler orquestra múltiplas operações
    // Validação
    var existingUser = await _db.UserProfiles...
    
    // Keycloak
    var keycloakUserId = await _keycloakService.GetUserIdByEmailAsync(...);
    if (string.IsNullOrEmpty(keycloakUserId))
    {
        keycloakUserId = await _keycloakService.CreateUserAsync(...);
    }
    
    // Criar UserProfile
    var userProfile = new UserProfile(...);
    _db.UserProfiles.Add(userProfile);
    await _db.SaveChangesAsync(...);
}
```

### **Solução: Criar Domain Service**

**1. Criar Domain Service:**
```csharp
// Domain/DomainServices/UserRegistrationService.cs
public class UserRegistrationService
{
    private readonly IApplicationDbContext _db;
    private readonly IKeycloakService _keycloakService;
    private readonly IClock _clock;
    private readonly ILogger<UserRegistrationService> _logger;

    public async Task<UserProfile> RegisterUserAsync(
        string email,
        string firstName,
        string lastName,
        string password,
        string realmId,
        CancellationToken cancellationToken)
    {
        // Validar email único
        var normalizedEmail = EmailAddress.Create(email).Value;
        var existingUser = await _db.UserProfiles
            .FirstOrDefaultAsync(u => u.Email.Value == normalizedEmail, cancellationToken);

        if (existingUser != null)
        {
            throw new DomainException("Domain:EmailJaCadastrado");
        }

        // Verificar/criar no Keycloak
        var keycloakUserId = await _keycloakService.GetUserIdByEmailAsync(realmId, email, cancellationToken);

        if (string.IsNullOrEmpty(keycloakUserId))
        {
            keycloakUserId = await _keycloakService.CreateUserAsync(
                realmId, email, firstName, lastName, password, cancellationToken);
            
            _logger.LogInformation("Usuário {Email} criado no Keycloak", email);
        }

        // Criar UserProfile
        var userId = Guid.NewGuid();
        var keycloakSubject = KeycloakSubject.Create(keycloakUserId);
        var emailVo = EmailAddress.Create(email);
        var displayName = PersonName.Create(firstName, lastName);

        var userProfile = new UserProfile(
            userId: userId,
            keycloakSubject: keycloakSubject,
            email: emailVo,
            displayName: displayName,
            clock: _clock);

        _db.UserProfiles.Add(userProfile);
        await _db.SaveChangesAsync(cancellationToken);

        return userProfile;
    }
}
```

**2. Refatorar Handler:**
```csharp
public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, RegisterUserResult>
{
    private readonly UserRegistrationService _userRegistrationService;
    private readonly IConfiguration _configuration;

    public async Task<RegisterUserResult> Handle(
        RegisterUserCommand request, 
        CancellationToken cancellationToken)
    {
        // ✅ SOLUÇÃO: Handler apenas orquestra
        var realmId = _configuration["Keycloak:Realm"] ?? "assistenteexecutivo";
        
        var userProfile = await _userRegistrationService.RegisterUserAsync(
            request.Email,
            request.FirstName,
            request.LastName,
            request.Password,
            realmId,
            cancellationToken);

        return new RegisterUserResult
        {
            UserId = userProfile.UserId,
            Email = request.Email,
            RealmId = realmId
        };
    }
}
```

---

## 📋 Checklist de Refatoração

Use este checklist ao refatorar cada controller/handler:

### **Controllers**
- [ ] Remover todas as dependências de `ApplicationDbContext`
- [ ] Usar apenas `IMediator` e `ILogger`
- [ ] Remover validações manuais (usar FluentValidation)
- [ ] Usar extension methods para obter `OwnerUserId` via MediatR
- [ ] Tratar exceções de forma consistente

### **Handlers**
- [ ] Handlers devem ser "thin" (apenas orquestrar)
- [ ] Mover lógica complexa para Domain Services
- [ ] Validações de entrada via FluentValidation (automático)
- [ ] Validações de negócio no Domain
- [ ] Publicar Domain Events após salvar

### **Domain Services**
- [ ] Criar services para lógica que envolve múltiplas entidades
- [ ] Services devem ser stateless
- [ ] Services devem receber interfaces do Domain (não Application)

---

## 🎯 Próximos Passos

1. **Implementar FluentValidation** em todos os Commands/Queries
2. **Criar Queries** para obter `OwnerUserId`
3. **Refatorar AuthController** para remover DbContext
4. **Criar UserRegistrationService** no Domain
5. **Refatorar todos os controllers** para remover DbContext





