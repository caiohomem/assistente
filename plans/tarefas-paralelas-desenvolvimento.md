# Tarefas Paralelas para Desenvolvimento

Este documento organiza as tarefas restantes em grupos que podem ser executados **em paralelo** por diferentes agentes.

## 🟢 Grupo A: Backend Core (Infraestrutura Base)
**Pode ser executado em paralelo, mas deve ser completado antes dos grupos B e C**

### A1: Keycloak Service & Provisioner
- ✅ `keycloak-service-complete` - Completar KeycloakService (corrigir erros, testar)
- ✅ `keycloak-provisioner-complete` - Completar KeycloakAdminProvisioner (idempotência, testes)

**Dependências:** Nenhuma (pode começar imediatamente)

### A2: Database & Domain
- ✅ `dbcontext-setup` - Criar DbContext e configuração EF Core para SQL Server
- ✅ `notifications-email-template-entity` - Criar entidade EmailTemplate no Domain
- ✅ `notifications-email-outbox` - Criar EmailOutboxMessage (opcional para MVP)

**Dependências:** Nenhuma (pode começar imediatamente)

### A3: Email Service
- ✅ `email-service-smtp` - Implementar EmailService com SMTP (single-tenant)
- ✅ `email-templates-seed` - Criar seed/migration com templates iniciais

**Dependências:** `notifications-email-template-entity` (deve esperar A2)

### A4: API Configuration
- ✅ `api-dependency-injection` - Configurar DI no Program.cs
- ✅ `api-appsettings` - Criar appsettings.Development.json

**Dependências:** `keycloak-service-complete`, `email-service-smtp` (pode começar parcialmente)

---

## 🔵 Grupo B: Auth BFF (Backend For Frontend)
**Depende do Grupo A, mas pode ser feito em paralelo com Grupo C**

### B1: Auth Endpoints Core
- ✅ `auth-bff-endpoints` - Criar endpoints: /auth/login, /auth/oauth-callback, /auth/session, /auth/logout
- ✅ `auth-bff-session` - Implementar gerenciamento de sessão (cookie HttpOnly, CSRF)

**Dependências:** `keycloak-service-complete`, `api-dependency-injection`

### B2: Password Reset
- ✅ `forgot-password-endpoint` - POST /auth/forgot-password
- ✅ `reset-password-endpoint` - POST /auth/reset-password

**Dependências:** `auth-bff-endpoints`, `email-service-smtp`, `notifications-email-template-entity`

---

## 🟡 Grupo C: Frontend (Web + Mobile)
**Pode ser feito em paralelo com Grupo B, mas precisa da API rodando**

### C1: Web Next.js
- ✅ `web-nextjs-setup` - Criar projeto Next.js em web/
- ✅ `web-login-pages` - Criar páginas: /login, /esqueci-senha, /reset-senha
- ✅ `web-protected-page` - Criar página protegida consumindo /auth/session

**Dependências:** `auth-bff-endpoints` (API deve estar rodando para testar)

### C2: Flutter Mobile
- ✅ `flutter-setup` - Criar projeto Flutter em app/
- ✅ `flutter-pkce-login` - Implementar login PKCE
- ✅ `flutter-protected-call` - Implementar chamada a endpoint protegido

**Dependências:** `auth-bff-endpoints` (API deve estar rodando para testar)

---

## 🟠 Grupo D: Infraestrutura DEV
**Pode ser feito em paralelo com qualquer grupo**

### D1: Docker & Environment
- ✅ `infra-docker-compose` - Criar docker-compose.yml (Keycloak + SQL Server + smtp4dev)
- ✅ `infra-env-docs` - Documentar variáveis e configuração do túnel

**Dependências:** Nenhuma (pode começar imediatamente)

---

## 🔴 Grupo E: Testes E2E
**Depende de todos os grupos anteriores**

### E1: Smoke Tests
- ✅ `e2e-smoke-tests` - Criar smoke tests completos

**Dependências:** Todos os grupos anteriores (A, B, C, D)

---

## 📊 Ordem Sugerida de Execução

### Fase 1: Fundação (Paralelo)
Execute em paralelo:
- **Agente 1:** A1 (Keycloak Service & Provisioner)
- **Agente 2:** A2 (Database & Domain)
- **Agente 3:** D1 (Docker & Environment)

### Fase 2: Serviços (Paralelo após A2)
Execute em paralelo:
- **Agente 1:** A3 (Email Service) - após A2
- **Agente 2:** A4 (API Configuration) - após A1 e A3 parcial

### Fase 3: Auth BFF (Paralelo após A1 e A4)
Execute em paralelo:
- **Agente 1:** B1 (Auth Endpoints Core)
- **Agente 2:** B2 (Password Reset) - após B1 e A3

### Fase 4: Frontend (Paralelo após B1)
Execute em paralelo:
- **Agente 1:** C1 (Web Next.js)
- **Agente 2:** C2 (Flutter Mobile)

### Fase 5: Testes (Após tudo)
- **Agente 1:** E1 (Smoke Tests)

---

## 🎯 Prioridades para MVP

### Crítico (deve estar pronto primeiro):
1. A1 - Keycloak Service & Provisioner
2. A2 - Database & Domain
3. A4 - API Configuration
4. B1 - Auth Endpoints Core
5. C1 - Web Next.js (pelo menos login)

### Importante (segunda onda):
6. A3 - Email Service
7. B2 - Password Reset
8. C1 completo - Web (todas as páginas)
9. D1 - Docker & Environment

### Opcional para MVP:
10. C2 - Flutter Mobile (pode ser feito depois)
11. A2 (EmailOutbox) - Opcional
12. E1 - Smoke Tests (pode ser manual inicialmente)

---

## 📝 Notas para Agentes

### Ao trabalhar em paralelo:
1. **Comunicação:** Se encontrar dependências não resolvidas, documente e avise
2. **Convenções:** Siga os padrões do sample `samples/clinica/`
3. **Testes:** Teste localmente antes de marcar como completo
4. **Commits:** Faça commits frequentes e pequenos

### Ao trabalhar em Keycloak:
- Use `PublicBaseUrl` para túnel: `https://auth.callback-local-cchagas.xyz`
- Realm padrão: `assistenteexecutivo`
- Client padrão: `assistenteexecutivo-app` (público, sem secret)

### Ao trabalhar em Email:
- Single-tenant (sem EnterpriseId)
- Templates em `EmailTemplate` entity
- SMTP configurável via appsettings

### Ao trabalhar em Frontend:
- Web: BFF com cookie HttpOnly
- Mobile: PKCE com Authorization Code
- Ambos consomem `/auth/session` para verificar autenticação

