# Plano de Implementação - Repositórios, Controllers e Interface Web

## Visão Geral

Este plano organiza as tarefas para implementar:
1. **Configurações EF Core** para todas as entidades do domínio
2. **Repositórios** para persistência
3. **Commands e Queries (CQRS)** na camada Application
4. **Controllers** na API
5. **Interface Web (Next.js)** para consumo da API

**Status Atual**:
- ✅ Domínio completo (100%)
- ✅ DbContext básico (apenas UserProfile, LoginAuditEntry, EmailTemplate)
- ✅ AuthController e MeController básicos
- ✅ Web básico (login, logout, páginas protegidas)
- ❌ Configurações EF Core para entidades CRM/Capture/Billing
- ❌ Repositórios (exceto EmailTemplateRepository)
- ❌ Commands/Queries
- ❌ Controllers para CRM/Capture/Billing
- ❌ Interface Web completa

---

## 🟢 GRUPO 1: Configurações EF Core (Fundação)

**Pode ser executado em paralelo por múltiplos agentes**

### T1.1: Configuração Contact
**ID**: T1.1  
**Arquivo**: `backend/src/AssistenteExecutivo.Infrastructure/Persistence/Configurations/ContactConfiguration.cs`

**Descrição**:
- Configurar entidade Contact com:
  - Tabela "Contacts"
  - Key: ContactId
  - Owned Types: PersonName (FirstName, LastName), Address (completo)
  - Collections: Emails (owned), Phones (owned), Tags (owned), Relationships (one-to-many)
  - Índices: OwnerUserId, CreatedAt, IsDeleted
  - Soft delete: IsDeleted

**Critérios de Aceite**:
- [ ] Configuração criada
- [ ] Owned types mapeados corretamente
- [ ] Collections configuradas
- [ ] Índices criados
- [ ] Compila sem erros

---

### T1.2: Configuração Relationship
**ID**: T1.2  
**Arquivo**: `backend/src/AssistenteExecutivo.Infrastructure/Persistence/Configurations/RelationshipConfiguration.cs`

**Descrição**:
- Configurar entidade Relationship com:
  - Tabela "Relationships"
  - Key: RelationshipId
  - Foreign Keys: SourceContactId, TargetContactId (ambos para Contacts)
  - Índices: SourceContactId, TargetContactId, Type
  - Unique constraint: SourceContactId + TargetContactId (se aplicável)

**Critérios de Aceite**:
- [ ] Configuração criada
- [ ] Foreign keys configuradas
- [ ] Índices criados
- [ ] Compila sem erros

---

### T1.3: Configuração Company
**ID**: T1.3  
**Arquivo**: `backend/src/AssistenteExecutivo.Infrastructure/Persistence/Configurations/CompanyConfiguration.cs`

**Descrição**:
- Configurar entidade Company com:
  - Tabela "Companies"
  - Key: CompanyId
  - Collection: Domains (JSON ou tabela separada)
  - Índices: Name (se busca por nome)

**Critérios de Aceite**:
- [ ] Configuração criada
- [ ] Collection de domínios configurada
- [ ] Índices criados
- [ ] Compila sem erros

---

### T1.4: Configuração Note
**ID**: T1.4  
**Arquivo**: `backend/src/AssistenteExecutivo.Infrastructure/Persistence/Configurations/NoteConfiguration.cs`

**Descrição**:
- Configurar entidade Note com:
  - Tabela "Notes"
  - Key: NoteId
  - Foreign Key: ContactId (para Contacts)
  - StructuredData como JSONB (PostgreSQL) ou NVARCHAR(MAX) (SQL Server)
  - Índices: ContactId, AuthorId, CreatedAt, Type

**Critérios de Aceite**:
- [ ] Configuração criada
- [ ] Foreign key configurada
- [ ] StructuredData como JSON
- [ ] Índices criados
- [ ] Compila sem erros

---

### T1.5: Configuração MediaAsset
**ID**: T1.5  
**Arquivo**: `backend/src/AssistenteExecutivo.Infrastructure/Persistence/Configurations/MediaAssetConfiguration.cs`

**Descrição**:
- Configurar entidade MediaAsset com:
  - Tabela "MediaAssets"
  - Key: MediaId
  - Owned Type: MediaRef (storageKey, hash, mimeType, size)
  - Metadata como JSON
  - Índices: OwnerUserId, Kind, CreatedAt

**Critérios de Aceite**:
- [ ] Configuração criada
- [ ] Owned type MediaRef mapeado
- [ ] Metadata como JSON
- [ ] Índices criados
- [ ] Compila sem erros

---

### T1.6: Configuração CaptureJob
**ID**: T1.6  
**Arquivo**: `backend/src/AssistenteExecutivo.Infrastructure/Persistence/Configurations/CaptureJobConfiguration.cs`

**Descrição**:
- Configurar entidade CaptureJob com:
  - Tabela "CaptureJobs"
  - Key: JobId
  - Foreign Keys: OwnerUserId (para UserProfiles), ContactId (opcional, para Contacts), MediaId (para MediaAssets)
  - Owned Types: OcrExtract, Transcript (quando aplicável)
  - Collections: ExtractedTasks (owned)
  - Índices: OwnerUserId, Status, Type, RequestedAt, ContactId

**Critérios de Aceite**:
- [ ] Configuração criada
- [ ] Foreign keys configuradas
- [ ] Owned types mapeados
- [ ] Collections configuradas
- [ ] Índices criados
- [ ] Compila sem erros

---

### T1.7: Configuração CreditWallet
**ID**: T1.7  
**Arquivo**: `backend/src/AssistenteExecutivo.Infrastructure/Persistence/Configurations/CreditWalletConfiguration.cs`

**Descrição**:
- Configurar entidade CreditWallet com:
  - Tabela "CreditWallets"
  - Key: OwnerUserId (PK e FK para UserProfiles)
  - Collection: Transactions (one-to-many para CreditTransaction)
  - Índices: OwnerUserId (já é PK)

**Critérios de Aceite**:
- [ ] Configuração criada
- [ ] Foreign key para UserProfiles
- [ ] Collection de transactions configurada
- [ ] Compila sem erros

---

### T1.8: Configuração CreditTransaction
**ID**: T1.8  
**Arquivo**: `backend/src/AssistenteExecutivo.Infrastructure/Persistence/Configurations/CreditTransactionConfiguration.cs`

**Descrição**:
- Configurar entidade CreditTransaction com:
  - Tabela "CreditTransactions"
  - Key: TransactionId
  - Foreign Key: OwnerUserId (para UserProfiles)
  - Owned Type: CreditAmount, IdempotencyKey
  - Índices: OwnerUserId, Type, OccurredAt, IdempotencyKey (unique quando não null)

**Critérios de Aceite**:
- [ ] Configuração criada
- [ ] Foreign key configurada
- [ ] Owned types mapeados
- [ ] Índice único em IdempotencyKey
- [ ] Compila sem erros

---

### T1.9: Atualizar ApplicationDbContext
**ID**: T1.9  
**Arquivo**: `backend/src/AssistenteExecutivo.Infrastructure/Persistence/ApplicationDbContext.cs`

**Descrição**:
- Adicionar DbSets para todas as entidades:
  - Contacts
  - Relationships
  - Companies
  - Notes
  - MediaAssets
  - CaptureJobs
  - CreditWallets
  - CreditTransactions
- Aplicar todas as configurações no OnModelCreating

**Critérios de Aceite**:
- [ ] Todos os DbSets adicionados
- [ ] Todas as configurações aplicadas via ApplyConfigurationsFromAssembly
- [ ] Compila sem erros
- [ ] Migration pode ser gerada

**Dependências**: T1.1 a T1.8 (pode ser feito após todas as configurações)

---

## 🔵 GRUPO 2: Repositórios (Paralelo após Grupo 1)

**Pode ser executado em paralelo por múltiplos agentes**

### T2.1: IContactRepository e ContactRepository
**ID**: T2.1  
**Arquivos**:
- `backend/src/AssistenteExecutivo.Application/Interfaces/IContactRepository.cs`
- `backend/src/AssistenteExecutivo.Infrastructure/Repositories/ContactRepository.cs`

**Métodos**:
- `GetByIdAsync(Guid contactId, Guid ownerUserId)`
- `GetAllAsync(Guid ownerUserId, bool includeDeleted = false)`
- `GetByEmailAsync(string email, Guid ownerUserId)`
- `GetByPhoneAsync(string phone, Guid ownerUserId)`
- `AddAsync(Contact contact)`
- `UpdateAsync(Contact contact)`
- `DeleteAsync(Contact contact)` (soft delete)
- `ExistsAsync(Guid contactId, Guid ownerUserId)`

**Critérios de Aceite**:
- [ ] Interface criada
- [ ] Repositório implementado
- [ ] Filtros por OwnerUserId em todas as queries
- [ ] Soft delete implementado
- [ ] Compila sem erros

**Dependências**: T1.1, T1.9

---

### T2.2: IRelationshipRepository e RelationshipRepository
**ID**: T2.2  
**Arquivos**:
- `backend/src/AssistenteExecutivo.Application/Interfaces/IRelationshipRepository.cs`
- `backend/src/AssistenteExecutivo.Infrastructure/Repositories/RelationshipRepository.cs`

**Métodos**:
- `GetByIdAsync(Guid relationshipId)`
- `GetByContactIdAsync(Guid contactId, Guid ownerUserId)`
- `GetBySourceAndTargetAsync(Guid sourceContactId, Guid targetContactId)`
- `AddAsync(Relationship relationship)`
- `UpdateAsync(Relationship relationship)`
- `DeleteAsync(Relationship relationship)`

**Critérios de Aceite**:
- [ ] Interface criada
- [ ] Repositório implementado
- [ ] Filtros por OwnerUserId
- [ ] Compila sem erros

**Dependências**: T1.2, T1.9

---

### T2.3: ICompanyRepository e CompanyRepository
**ID**: T2.3  
**Arquivos**:
- `backend/src/AssistenteExecutivo.Application/Interfaces/ICompanyRepository.cs`
- `backend/src/AssistenteExecutivo.Infrastructure/Repositories/CompanyRepository.cs`

**Métodos**:
- `GetByIdAsync(Guid companyId)`
- `GetByNameAsync(string name)`
- `GetByDomainAsync(string domain)`
- `AddAsync(Company company)`
- `UpdateAsync(Company company)`
- `ExistsAsync(Guid companyId)`

**Critérios de Aceite**:
- [ ] Interface criada
- [ ] Repositório implementado
- [ ] Busca por domínio implementada
- [ ] Compila sem erros

**Dependências**: T1.3, T1.9

---

### T2.4: INoteRepository e NoteRepository
**ID**: T2.4  
**Arquivos**:
- `backend/src/AssistenteExecutivo.Application/Interfaces/INoteRepository.cs`
- `backend/src/AssistenteExecutivo.Infrastructure/Repositories/NoteRepository.cs`

**Métodos**:
- `GetByIdAsync(Guid noteId)`
- `GetByContactIdAsync(Guid contactId, Guid ownerUserId)`
- `GetByAuthorIdAsync(Guid authorId, Guid ownerUserId)`
- `AddAsync(Note note)`
- `UpdateAsync(Note note)`
- `DeleteAsync(Note note)`

**Critérios de Aceite**:
- [ ] Interface criada
- [ ] Repositório implementado
- [ ] Filtros por OwnerUserId
- [ ] Compila sem erros

**Dependências**: T1.4, T1.9

---

### T2.5: IMediaAssetRepository e MediaAssetRepository
**ID**: T2.5  
**Arquivos**:
- `backend/src/AssistenteExecutivo.Application/Interfaces/IMediaAssetRepository.cs`
- `backend/src/AssistenteExecutivo.Infrastructure/Repositories/MediaAssetRepository.cs`

**Métodos**:
- `GetByIdAsync(Guid mediaId, Guid ownerUserId)`
- `GetByHashAsync(string hash, Guid ownerUserId)`
- `GetAllByOwnerAsync(Guid ownerUserId)`
- `AddAsync(MediaAsset mediaAsset)`
- `DeleteAsync(MediaAsset mediaAsset)`

**Critérios de Aceite**:
- [ ] Interface criada
- [ ] Repositório implementado
- [ ] Busca por hash (deduplicação)
- [ ] Filtros por OwnerUserId
- [ ] Compila sem erros

**Dependências**: T1.5, T1.9

---

### T2.6: ICaptureJobRepository e CaptureJobRepository
**ID**: T2.6  
**Arquivos**:
- `backend/src/AssistenteExecutivo.Application/Interfaces/ICaptureJobRepository.cs`
- `backend/src/AssistenteExecutivo.Infrastructure/Repositories/CaptureJobRepository.cs`

**Métodos**:
- `GetByIdAsync(Guid jobId, Guid ownerUserId)`
- `GetByStatusAsync(JobStatus status, Guid ownerUserId)`
- `GetByContactIdAsync(Guid contactId, Guid ownerUserId)`
- `GetByMediaIdAsync(Guid mediaId, Guid ownerUserId)`
- `AddAsync(CaptureJob job)`
- `UpdateAsync(CaptureJob job)`

**Critérios de Aceite**:
- [ ] Interface criada
- [ ] Repositório implementado
- [ ] Filtros por OwnerUserId
- [ ] Compila sem erros

**Dependências**: T1.6, T1.9

---

### T2.7: ICreditWalletRepository e CreditWalletRepository
**ID**: T2.7  
**Arquivos**:
- `backend/src/AssistenteExecutivo.Application/Interfaces/ICreditWalletRepository.cs`
- `backend/src/AssistenteExecutivo.Infrastructure/Repositories/CreditWalletRepository.cs`

**Métodos**:
- `GetByOwnerIdAsync(Guid ownerUserId)`
- `AddAsync(CreditWallet wallet)`
- `UpdateAsync(CreditWallet wallet)`
- `GetOrCreateAsync(Guid ownerUserId)` (cria se não existir)

**Critérios de Aceite**:
- [ ] Interface criada
- [ ] Repositório implementado
- [ ] GetOrCreate implementado
- [ ] Compila sem erros

**Dependências**: T1.7, T1.9

---

### T2.8: Registrar Repositórios no DI
**ID**: T2.8  
**Arquivo**: `backend/src/AssistenteExecutivo.Infrastructure/DependencyInjection.cs`

**Descrição**:
- Registrar todos os repositórios no container de DI:
  - IContactRepository -> ContactRepository
  - IRelationshipRepository -> RelationshipRepository
  - ICompanyRepository -> CompanyRepository
  - INoteRepository -> NoteRepository
  - IMediaAssetRepository -> MediaAssetRepository
  - ICaptureJobRepository -> CaptureJobRepository
  - ICreditWalletRepository -> CreditWalletRepository

**Critérios de Aceite**:
- [ ] Todos os repositórios registrados
- [ ] Scoped lifetime
- [ ] Compila sem erros

**Dependências**: T2.1 a T2.7

---

## 🟡 GRUPO 3: Commands (CQRS) - Application Layer

**Pode ser executado em paralelo após Grupo 2**

### T3.1: Commands - Contact
**ID**: T3.1  
**Arquivos**:
- `backend/src/AssistenteExecutivo.Application/Commands/Contacts/CreateContactCommand.cs`
- `backend/src/AssistenteExecutivo.Application/Commands/Contacts/UpdateContactCommand.cs`
- `backend/src/AssistenteExecutivo.Application/Commands/Contacts/DeleteContactCommand.cs`
- `backend/src/AssistenteExecutivo.Application/Commands/Contacts/AddContactEmailCommand.cs`
- `backend/src/AssistenteExecutivo.Application/Commands/Contacts/AddContactPhoneCommand.cs`
- `backend/src/AssistenteExecutivo.Application/Commands/Contacts/AddContactTagCommand.cs`
- `backend/src/AssistenteExecutivo.Application/Commands/Contacts/AddContactRelationshipCommand.cs`
- Handlers correspondentes em `Handlers/Contacts/`

**Descrição**:
- Criar commands e handlers para operações de Contact
- Usar MediatR
- Validar OwnerUserId
- Publicar eventos de domínio

**Critérios de Aceite**:
- [ ] Commands criados
- [ ] Handlers implementados
- [ ] Validações de domínio
- [ ] Eventos de domínio publicados
- [ ] Compila sem erros

**Dependências**: T2.1

---

### T3.2: Commands - Capture (Upload Card)
**ID**: T3.2  
**Arquivos**:
- `backend/src/AssistenteExecutivo.Application/Commands/Capture/UploadCardCommand.cs`
- `backend/src/AssistenteExecutivo.Application/Commands/Capture/ProcessAudioNoteCommand.cs`
- Handlers correspondentes

**Descrição**:
- UploadCardCommand: criar MediaAsset, criar CaptureJob, processar OCR (via port), criar/atualizar Contact
- ProcessAudioNoteCommand: criar MediaAsset, criar CaptureJob, processar áudio (via port), criar Note
- Consumir créditos quando aplicável

**Critérios de Aceite**:
- [ ] Commands criados
- [ ] Handlers implementados
- [ ] Integração com ports (IOcrProvider, ISpeechToTextProvider)
- [ ] Consumo de créditos
- [ ] Compila sem erros

**Dependências**: T2.1, T2.5, T2.6, T2.7

---

### T3.3: Commands - Notes
**ID**: T3.3  
**Arquivos**:
- `backend/src/AssistenteExecutivo.Application/Commands/Notes/CreateTextNoteCommand.cs`
- `backend/src/AssistenteExecutivo.Application/Commands/Notes/CreateAudioNoteCommand.cs`
- `backend/src/AssistenteExecutivo.Application/Commands/Notes/UpdateNoteCommand.cs`
- Handlers correspondentes

**Descrição**:
- Criar commands e handlers para operações de Note
- Validar ContactId e AuthorId

**Critérios de Aceite**:
- [ ] Commands criados
- [ ] Handlers implementados
- [ ] Validações
- [ ] Compila sem erros

**Dependências**: T2.1, T2.4

---

### T3.4: Commands - CreditWallet
**ID**: T3.4  
**Arquivos**:
- `backend/src/AssistenteExecutivo.Application/Commands/Credits/GrantCreditsCommand.cs`
- `backend/src/AssistenteExecutivo.Application/Commands/Credits/ReserveCreditsCommand.cs`
- `backend/src/AssistenteExecutivo.Application/Commands/Credits/ConsumeCreditsCommand.cs`
- `backend/src/AssistenteExecutivo.Application/Commands/Credits/RefundCreditsCommand.cs`
- Handlers correspondentes

**Descrição**:
- Criar commands e handlers para operações de créditos
- Validar idempotência
- Validar saldo

**Critérios de Aceite**:
- [ ] Commands criados
- [ ] Handlers implementados
- [ ] Idempotência validada
- [ ] Compila sem erros

**Dependências**: T2.7

---

### T3.5: Configurar MediatR
**ID**: T3.5  
**Arquivo**: `backend/src/AssistenteExecutivo.Infrastructure/DependencyInjection.cs`

**Descrição**:
- Adicionar MediatR ao DI
- Registrar handlers automaticamente

**Critérios de Aceite**:
- [ ] MediatR configurado
- [ ] Handlers registrados
- [ ] Compila sem erros

**Dependências**: T3.1 a T3.4 (pode ser feito antes, mas precisa dos handlers)

---

## 🟠 GRUPO 4: Queries (CQRS) - Application Layer

**Pode ser executado em paralelo após Grupo 2**

### T4.1: Queries - Contact
**ID**: T4.1  
**Arquivos**:
- `backend/src/AssistenteExecutivo.Application/Queries/Contacts/GetContactByIdQuery.cs`
- `backend/src/AssistenteExecutivo.Application/Queries/Contacts/ListContactsQuery.cs`
- `backend/src/AssistenteExecutivo.Application/Queries/Contacts/SearchContactsQuery.cs`
- Handlers correspondentes
- DTOs em `DTOs/ContactDto.cs`

**Descrição**:
- Criar queries e handlers para leitura de Contact
- Criar DTOs para resposta
- Filtros por OwnerUserId

**Critérios de Aceite**:
- [ ] Queries criadas
- [ ] Handlers implementados
- [ ] DTOs criados
- [ ] Filtros implementados
- [ ] Compila sem erros

**Dependências**: T2.1

---

### T4.2: Queries - Notes
**ID**: T4.2  
**Arquivos**:
- `backend/src/AssistenteExecutivo.Application/Queries/Notes/GetNoteByIdQuery.cs`
- `backend/src/AssistenteExecutivo.Application/Queries/Notes/ListNotesByContactQuery.cs`
- Handlers correspondentes
- DTOs em `DTOs/NoteDto.cs`

**Descrição**:
- Criar queries e handlers para leitura de Note
- Criar DTOs

**Critérios de Aceite**:
- [ ] Queries criadas
- [ ] Handlers implementados
- [ ] DTOs criados
- [ ] Compila sem erros

**Dependências**: T2.4

---

### T4.3: Queries - CaptureJobs
**ID**: T4.3  
**Arquivos**:
- `backend/src/AssistenteExecutivo.Application/Queries/Capture/GetCaptureJobByIdQuery.cs`
- `backend/src/AssistenteExecutivo.Application/Queries/Capture/ListCaptureJobsQuery.cs`
- Handlers correspondentes
- DTOs em `DTOs/CaptureJobDto.cs`

**Descrição**:
- Criar queries e handlers para leitura de CaptureJob
- Criar DTOs

**Critérios de Aceite**:
- [ ] Queries criadas
- [ ] Handlers implementados
- [ ] DTOs criados
- [ ] Compila sem erros

**Dependências**: T2.6

---

### T4.4: Queries - CreditWallet
**ID**: T4.4  
**Arquivos**:
- `backend/src/AssistenteExecutivo.Application/Queries/Credits/GetCreditBalanceQuery.cs`
- `backend/src/AssistenteExecutivo.Application/Queries/Credits/ListCreditTransactionsQuery.cs`
- Handlers correspondentes
- DTOs em `DTOs/CreditWalletDto.cs`

**Descrição**:
- Criar queries e handlers para leitura de CreditWallet
- Criar DTOs

**Critérios de Aceite**:
- [ ] Queries criadas
- [ ] Handlers implementados
- [ ] DTOs criados
- [ ] Compila sem erros

**Dependências**: T2.7

---

## 🔴 GRUPO 5: Controllers - API

**Pode ser executado em paralelo após Grupos 3 e 4**

### T5.1: ContactsController
**ID**: T5.1  
**Arquivo**: `backend/src/AssistenteExecutivo.Api/Controllers/ContactsController.cs`

**Endpoints**:
- `GET /api/contacts` - Listar contatos (ListContactsQuery)
- `GET /api/contacts/{id}` - Obter contato por ID (GetContactByIdQuery)
- `POST /api/contacts` - Criar contato (CreateContactCommand)
- `PUT /api/contacts/{id}` - Atualizar contato (UpdateContactCommand)
- `DELETE /api/contacts/{id}` - Deletar contato (DeleteContactCommand)
- `POST /api/contacts/{id}/emails` - Adicionar email (AddContactEmailCommand)
- `POST /api/contacts/{id}/phones` - Adicionar telefone (AddContactPhoneCommand)
- `POST /api/contacts/{id}/tags` - Adicionar tag (AddContactTagCommand)
- `POST /api/contacts/{id}/relationships` - Adicionar relacionamento (AddContactRelationshipCommand)

**Descrição**:
- Criar controller com todos os endpoints
- Autenticação obrigatória
- Extrair OwnerUserId do token/sessão
- Validação de modelos
- Tratamento de erros

**Critérios de Aceite**:
- [ ] Controller criado
- [ ] Todos os endpoints implementados
- [ ] Autenticação configurada
- [ ] Validações implementadas
- [ ] Swagger documentado
- [ ] Compila sem erros

**Dependências**: T3.1, T4.1

---

### T5.2: CaptureController
**ID**: T5.2  
**Arquivo**: `backend/src/AssistenteExecutivo.Api/Controllers/CaptureController.cs`

**Endpoints**:
- `POST /api/capture/upload-card` - Upload de cartão (UploadCardCommand)
- `POST /api/capture/audio-note` - Processar nota de áudio (ProcessAudioNoteCommand)
- `GET /api/capture/jobs/{id}` - Obter job por ID (GetCaptureJobByIdQuery)
- `GET /api/capture/jobs` - Listar jobs (ListCaptureJobsQuery)

**Descrição**:
- Criar controller para operações de captura
- Upload de arquivos (multipart/form-data)
- Processamento assíncrono
- Retornar job ID para acompanhamento

**Critérios de Aceite**:
- [ ] Controller criado
- [ ] Upload de arquivos funcionando
- [ ] Processamento assíncrono
- [ ] Compila sem erros

**Dependências**: T3.2, T4.3

---

### T5.3: NotesController
**ID**: T5.3  
**Arquivo**: `backend/src/AssistenteExecutivo.Api/Controllers/NotesController.cs`

**Endpoints**:
- `GET /api/contacts/{contactId}/notes` - Listar notas do contato (ListNotesByContactQuery)
- `GET /api/notes/{id}` - Obter nota por ID (GetNoteByIdQuery)
- `POST /api/contacts/{contactId}/notes` - Criar nota de texto (CreateTextNoteCommand)
- `PUT /api/notes/{id}` - Atualizar nota (UpdateNoteCommand)

**Descrição**:
- Criar controller para operações de notas
- Filtros por ContactId

**Critérios de Aceite**:
- [x] Controller criado
- [x] Todos os endpoints implementados
- [x] Compila sem erros

**Dependências**: T3.3, T4.2

---

### T5.4: CreditsController
**ID**: T5.4  
**Arquivo**: `backend/src/AssistenteExecutivo.Api/Controllers/CreditsController.cs`

**Endpoints**:
- `GET /api/credits/balance` - Obter saldo (GetCreditBalanceQuery)
- `GET /api/credits/transactions` - Listar transações (ListCreditTransactionsQuery)
- `POST /api/credits/grant` - Conceder créditos (GrantCreditsCommand) - Admin only

**Descrição**:
- Criar controller para operações de créditos
- Admin only para grant

**Critérios de Aceite**:
- [ ] Controller criado
- [ ] Endpoints implementados
- [ ] Autorização configurada
- [ ] Compila sem erros

**Dependências**: T3.4, T4.4

---

## 🟣 GRUPO 6: Interface Web (Next.js)

**Pode ser executado em paralelo após Grupo 5**

### T6.1: Setup Base e Types
**ID**: T6.1  
**Arquivos**:
- `web/src/lib/types/contact.ts`
- `web/src/lib/types/note.ts`
- `web/src/lib/types/capture.ts`
- `web/src/lib/types/credit.ts`
- `web/src/lib/api/contactsApi.ts`
- `web/src/lib/api/notesApi.ts`
- `web/src/lib/api/captureApi.ts`
- `web/src/lib/api/creditsApi.ts`

**Descrição**:
- Criar types TypeScript baseados nos DTOs
- Criar API clients usando o BFF helper existente
- Configurar interceptors para autenticação

**Critérios de Aceite**:
- [x] Types criados
- [x] API clients criados
- [x] Autenticação configurada
- [x] Compila sem erros

**Dependências**: T5.1, T5.2, T5.3, T5.4 (pode começar parcialmente)

---

### T6.2: Página de Contatos (Listagem)
**ID**: T6.2  
**Arquivo**: `web/src/app/contatos/page.tsx`

**Descrição**:
- Página de listagem de contatos
- Busca/filtros
- Paginação
- Link para detalhes
- Botão para criar novo

**Critérios de Aceite**:
- [ ] Página criada
- [ ] Listagem funcionando
- [ ] Busca implementada
- [ ] Paginação implementada
- [ ] Design responsivo

**Dependências**: T6.1

---

### T6.3: Página de Detalhes do Contato
**ID**: T6.3  
**Arquivo**: `web/src/app/contatos/[id]/page.tsx`

**Descrição**:
- Página de detalhes do contato
- Exibir todas as informações
- Lista de notas
- Lista de relacionamentos
- Botões para editar, adicionar nota, adicionar relacionamento

**Critérios de Aceite**:
- [ ] Página criada
- [ ] Detalhes exibidos
- [ ] Lista de notas
- [ ] Lista de relacionamentos
- [ ] Ações funcionando

**Dependências**: T6.1, T6.4 (parcial)

---

### T6.4: Formulários de Contato
**ID**: T6.4  
**Arquivos**:
- `web/src/app/contatos/novo/page.tsx`
- `web/src/app/contatos/[id]/editar/page.tsx`
- `web/src/components/ContactForm.tsx`

**Descrição**:
- Formulário para criar/editar contato
- Campos: nome, emails, telefones, empresa, cargo, endereço
- Validação
- Submit para API

**Critérios de Aceite**:
- [ ] Formulário criado
- [ ] Validação implementada
- [ ] Submit funcionando
- [ ] Feedback de sucesso/erro

**Dependências**: T6.1

---

### T6.5: Upload de Cartão (OCR)
**ID**: T6.5  
**Arquivo**: `web/src/app/contatos/upload-cartao/page.tsx`

**Descrição**:
- Página para upload de imagem de cartão
- Preview da imagem
- Exibir resultado do OCR
- Permitir edição antes de criar contato
- Criar contato a partir do resultado

**Critérios de Aceite**:
- [ ] Página criada
- [ ] Upload funcionando
- [ ] Preview da imagem
- [ ] Resultado do OCR exibido
- [ ] Edição permitida
- [ ] Criação de contato funcionando

**Dependências**: T6.1

---

### T6.6: Notas de Áudio
**ID**: T6.6  
**Arquivo**: `web/src/app/contatos/[id]/notas-audio/page.tsx`

**Descrição**:
- Página para upload de áudio
- Gravação de áudio (opcional)
- Upload de arquivo
- Exibir status do processamento
- Exibir transcrição e resumo quando pronto

**Critérios de Aceite**:
- [ ] Página criada
- [ ] Upload funcionando
- [ ] Status do job exibido
- [ ] Polling para atualizar status
- [ ] Resultado exibido quando pronto

**Dependências**: T6.1

---

### T6.7: Dashboard/Saldo de Créditos
**ID**: T6.7  
**Arquivo**: `web/src/app/dashboard/page.tsx`

**Descrição**:
- Dashboard com resumo
- Saldo de créditos
- Últimas atividades
- Estatísticas básicas

**Critérios de Aceite**:
- [ ] Página criada
- [ ] Saldo exibido
- [ ] Estatísticas exibidas
- [ ] Design responsivo

**Dependências**: T6.1

---

## 🔧 GRUPO 7: Migrations e Testes

**Pode ser executado em paralelo após Grupos 1 e 2**

### T7.1: Criar Migration Inicial
**ID**: T7.1  
**Comando**: `dotnet ef migrations add InitialDomainEntities`

**Descrição**:
- Gerar migration com todas as entidades
- Revisar migration gerada
- Ajustar se necessário

**Critérios de Aceite**:
- [x] Migration criada
- [x] Revisada e ajustada
- [x] Pode ser aplicada ao banco

**Dependências**: T1.1 a T1.9

---

### T7.2: Testes de Integração - Repositórios
**ID**: T7.2  
**Arquivos**: `backend/tests/AssistenteExecutivo.Infrastructure.Tests/Repositories/*Tests.cs`

**Descrição**:
- Criar testes de integração para cada repositório
- Usar Testcontainers ou banco em memória
- Testar CRUD básico

**Critérios de Aceite**:
- [ ] Testes criados
- [ ] Todos os repositórios testados
- [ ] Testes passando

**Dependências**: T2.1 a T2.7

---

### T7.3: Testes de Integração - Handlers
**ID**: T7.3  
**Arquivos**: `backend/tests/AssistenteExecutivo.Application.Tests/Handlers/*Tests.cs`

**Descrição**:
- Criar testes de integração para handlers principais
- Testar fluxo completo

**Critérios de Aceite**:
- [x] Testes criados
- [x] Handlers principais testados
- [x] Testes passando

**Dependências**: T3.1 a T3.4, T4.1 a T4.4

**Implementação**:
- Projeto `AssistenteExecutivo.Application.Tests` criado
- Base class `HandlerTestBase` com setup de banco em memória e mocks
- Testes para handlers de Contact: Create, GetById, Update, Delete
- Testes para handlers de Credits: Grant, Consume, Reserve, Refund
- Testes para handlers de Notes: CreateTextNote, CreateAudioNote
- Testes para handlers de Capture: UploadCard, ProcessAudioNote

---

## 📊 Ordem de Execução Sugerida

### Fase 1: Fundação (Paralelo)
Execute em paralelo:
- **Agente 1**: T1.1, T1.2, T1.3 (Configurações Contact, Relationship, Company)
- **Agente 2**: T1.4, T1.5, T1.6 (Configurações Note, MediaAsset, CaptureJob)
- **Agente 3**: T1.7, T1.8 (Configurações CreditWallet, CreditTransaction)

### Fase 2: Finalizar Fundação
- **Agente 1**: T1.9 (Atualizar ApplicationDbContext)
- **Agente 2**: T7.1 (Criar Migration)

### Fase 3: Repositórios (Paralelo)
Execute em paralelo:
- **Agente 1**: T2.1, T2.2 (Contact, Relationship)
- **Agente 2**: T2.3, T2.4 (Company, Note)
- **Agente 3**: T2.5, T2.6 (MediaAsset, CaptureJob)
- **Agente 4**: T2.7 (CreditWallet)

### Fase 4: Registrar Repositórios
- **Agente 1**: T2.8 (Registrar no DI)

### Fase 5: Application Layer (Paralelo)
Execute em paralelo:
- **Agente 1**: T3.1 (Commands Contact)
- **Agente 2**: T3.2 (Commands Capture)
- **Agente 3**: T3.3 (Commands Notes)
- **Agente 4**: T3.4 (Commands Credits)
- **Agente 5**: T4.1 (Queries Contact)
- **Agente 6**: T4.2 (Queries Notes)
- **Agente 7**: T4.3 (Queries CaptureJobs)
- **Agente 8**: T4.4 (Queries Credits)

### Fase 6: Configurar MediatR
- **Agente 1**: T3.5 (Configurar MediatR)

### Fase 7: Controllers (Paralelo)
Execute em paralelo:
- **Agente 1**: T5.1 (ContactsController)
- **Agente 2**: T5.2 (CaptureController)
- **Agente 3**: T5.3 (NotesController)
- **Agente 4**: T5.4 (CreditsController)

### Fase 8: Interface Web (Paralelo)
Execute em paralelo:
- **Agente 1**: T6.1 (Setup Base e Types)
- **Agente 2**: T6.2 (Listagem Contatos) - após T6.1
- **Agente 3**: T6.4 (Formulários) - após T6.1
- **Agente 4**: T6.5 (Upload Cartão) - após T6.1
- **Agente 5**: T6.6 (Notas Áudio) - após T6.1
- **Agente 6**: T6.7 (Dashboard) - após T6.1

### Fase 9: Detalhes e Testes
- **Agente 1**: T6.3 (Detalhes Contato) - após T6.2, T6.4
- **Agente 2**: T7.2 (Testes Repositórios)
- **Agente 3**: T7.3 (Testes Handlers)

---

## 🎯 Prioridades para MVP

### Crítico (deve estar pronto primeiro):
1. T1.1 a T1.9 - Configurações EF Core
2. T2.1, T2.5, T2.6 - Repositórios Contact, MediaAsset, CaptureJob
3. T3.1, T3.2 - Commands Contact e Capture
4. T4.1 - Queries Contact
5. T5.1, T5.2 - Controllers Contact e Capture
6. T6.1, T6.2, T6.4, T6.5 - Web: Types, Listagem, Formulários, Upload

### Importante (segunda onda):
7. T2.4, T3.3, T4.2, T5.3, T6.6 - Notes completo
8. T2.7, T3.4, T4.4, T5.4, T6.7 - Credits completo
9. T6.3 - Detalhes do Contato

### Opcional para MVP:
10. T2.2, T2.3 - Repositórios Relationship e Company
11. T7.2, T7.3 - Testes de integração

---

## 📝 Notas para Agentes

### Ao trabalhar em Configurações EF Core:
- Seguir padrão do `samples/clinica`
- Usar `OwnsOne` para Value Objects
- Usar `OwnsMany` para collections de Value Objects
- Configurar índices apropriados
- Configurar soft delete quando aplicável

### Ao trabalhar em Repositórios:
- Sempre filtrar por OwnerUserId
- Usar async/await
- Retornar IReadOnlyCollection
- Implementar soft delete quando aplicável

### Ao trabalhar em Commands/Queries:
- Usar MediatR
- Validar OwnerUserId
- Publicar eventos de domínio
- Usar DTOs para retorno

### Ao trabalhar em Controllers:
- Autenticação obrigatória
- Extrair OwnerUserId do token/sessão
- Validação de modelos
- Tratamento de erros padronizado
- Documentar Swagger

### Ao trabalhar em Web:
- Usar BFF helper existente (`web/src/lib/bff.ts`)
- TypeScript strict
- Validação de formulários
- Feedback de loading/erro
- Design responsivo

---

## 🔗 Referências

- **Sample de referência**: `samples/clinica/`
- **Documentação EF Core**: https://learn.microsoft.com/en-us/ef/core/
- **MediatR**: https://github.com/jbogard/MediatR
- **Next.js**: https://nextjs.org/docs

