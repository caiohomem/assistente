# Problema de Autenticação e Redirects Infinitos - Descrição Detalhada

## Contexto do Sistema

### Arquitetura
- **Frontend**: Next.js (React) rodando em `assistente.callback-local-cchagas.xyz`
- **Backend**: ASP.NET Core 8.0 API rodando em `assistente-api.callback-local-cchagas.xyz`
- **Autenticação**: Keycloak (OAuth2 Authorization Code Flow)
- **Padrão**: BFF (Backend for Frontend) - sessões server-side com cookies
- **Armazenamento de Sessão**: SQL Server (DistributedCache)

### Fluxo de Autenticação Esperado
1. Usuário acessa `/login` no frontend
2. Frontend redireciona para `/auth/login` no backend
3. Backend redireciona para Keycloak
4. Usuário faz login no Keycloak
5. Keycloak redireciona para `/auth/callback` no backend
6. Backend armazena tokens na sessão, define cookie `ae.sid` e redireciona para frontend
7. Frontend verifica sessão e redireciona para dashboard
8. Requisições subsequentes incluem cookie `ae.sid` para autenticação

## Problema Observado

### Sintomas Principais

1. **Redirects Infinitos Após Login**
   - Após login bem-sucedido no Keycloak, o site entra em loop de redirects
   - A página "pisca" continuamente entre `/login` e `/dashboard`
   - Console do navegador mostra: `[Login] Redirecionamento recente detectado, aguardando...`

2. **Cookie de Sessão Não Está Sendo Enviado**
   - Logs do frontend mostram: `hasCookieHeader: false`, `cookiesAvailable: ""` ou apenas `XSRF-TOKEN`
   - Cookie `ae.sid` não aparece em `document.cookie` no navegador
   - Requisições para `/auth/session` não incluem o cookie de sessão

3. **API Retorna `authenticated: true` Sem Cookie**
   - Endpoint `/auth/session` retorna `authenticated: true` mesmo quando não há cookie `ae.sid`
   - Isso sugere que há cache de sessão antigo ou verificação incorreta

4. **Erro "Usuário não autenticado" em Endpoints Protegidos**
   - Após conseguir passar pela tela de login, requisições para `/api/credits/*` falham
   - Erro: `System.UnauthorizedAccessException: Usuário não autenticado ou não encontrado no sistema.`
   - Stack trace aponta para `HttpContextExtensions.GetRequiredOwnerUserIdAsync`
   - `BffSessionAuthenticationHandler` não está autenticando as requisições

5. **Usuário com Status `Deleted`**
   - No banco de dados, o usuário `caio.work@gmail.com` está com `Status = Deleted`
   - Isso pode estar impedindo a autenticação mesmo que o KeycloakSubject seja encontrado

## Análise do Código Atual

### Frontend (`web/src/app/login/page.tsx`)
- Convertido para Client-Side Component (`'use client'`)
- Verifica cookie `ae.sid` antes de considerar autenticado
- Usa `sessionStorage` para prevenir loops de redirect
- Usa `window.location.href` para redirects completos
- Logs mostram que `hasCookies: false` na maioria das vezes

### Backend - Endpoint Session (`backend/src/AssistenteExecutivo.Api/Controllers/AuthController.cs`)
- Verifica presença do cookie `ae.sid` no header e em `Request.Cookies`
- Se não houver cookie, retorna `authenticated: false`
- Se houver cookie, verifica `BffSessionStore.IsAuthenticated()`
- **Problema**: Logs do backend não aparecem no arquivo de debug, sugerindo que o backend não foi reiniciado ou logs não estão sendo escritos

### Backend - OAuth Callback (`AuthController.OAuthCallback`)
- Armazena tokens e dados do usuário na sessão
- Chama `HttpContext.Session.CommitAsync()` antes de redirecionar
- Define cookie CSRF (`XSRF-TOKEN`)
- **Problema**: Não há logs confirmando que o cookie `ae.sid` está sendo definido no `Set-Cookie` header

### Backend - Session Middleware (`Program.cs`)
- Configurado com `SameSiteMode.None` e `CookieSecurePolicy.Always`
- Cookie nomeado como `ae.sid`
- Domain configurado para funcionar entre subdomínios
- **Problema**: Configuração pode não estar sendo aplicada corretamente

### Backend - Authentication Handler (`BffSessionAuthenticationHandler.cs`)
- Verifica `BffSessionStore.IsAuthenticated()` na sessão
- Cria `ClaimsPrincipal` com dados da sessão
- **Problema**: Não está sendo chamado ou não está encontrando sessão autenticada

### Backend - GetOwnerUserIdQueryHandler
- Busca `UserProfile` pelo `KeycloakSubject`
- Não filtra por status (deveria encontrar usuário deletado)
- **Problema**: Pode estar retornando `null` se o usuário não for encontrado ou se houver erro

## Hipóteses Sobre a Causa Raiz

### H1: Cookie Não Está Sendo Definido Após OAuth Callback
- O middleware de sessão pode não estar definindo o cookie corretamente
- O `Set-Cookie` header pode não estar sendo enviado na resposta
- O cookie pode estar sendo definido com configurações incorretas (Domain, SameSite, Secure)

### H2: Cookie Não Está Sendo Enviado nas Requisições
- Problema de SameSite/Domain/Secure impedindo o navegador de enviar o cookie
- Cookie pode estar sendo definido para um domínio diferente
- Navegador pode estar bloqueando cookies de terceiros

### H3: Backend Não Foi Reiniciado
- Código antigo ainda está em execução
- Mudanças no código não foram aplicadas
- Logs do backend não aparecem porque o código antigo não tem instrumentação

### H4: Cache de Sessão Retornando Dados Antigos
- SQL Server cache pode ter sessão antiga sem cookie válido
- Sessão pode estar sendo criada sem cookie sendo definido
- `BffSessionStore.IsAuthenticated()` pode retornar `true` para sessão vazia

### H5: Usuário Deletado Impedindo Autenticação
- `GetOwnerUserIdQueryHandler` encontra o usuário, mas algum outro código verifica status
- Usuário deletado não deveria conseguir fazer login no Keycloak, mas consegue
- Sistema deveria rejeitar login de usuário deletado antes de criar sessão

### H6: BffSessionAuthenticationHandler Não Está Sendo Chamado
- Ordem dos middlewares pode estar incorreta
- Handler pode estar retornando `NoResult()` porque não encontra sessão autenticada
- Sessão pode não estar sendo carregada corretamente do cache

## Evidências dos Logs

### Logs do Frontend (última execução)
```json
{"location":"bff.ts:51","message":"Before fetch session","data":{"hasCookieHeader":false,"cookiesAvailable":"","cookieCount":1}}
{"location":"bff.ts:88","message":"Session response received","data":{"authenticated":true,"hasUser":true,"userEmail":"caio.work@gmail.com"}}
```

**Observação**: API retorna `authenticated: true` mesmo sem cookie sendo enviado.

### Logs do Backend
- **Nenhum log encontrado** no arquivo `debug.log`
- Isso indica que o backend não foi reiniciado ou os logs não estão sendo escritos

## Arquivos Relevantes

### Frontend
- `web/src/app/login/page.tsx` - Página de login com verificação de autenticação
- `web/src/lib/bff.ts` - Funções utilitárias para interagir com BFF API

### Backend
- `backend/src/AssistenteExecutivo.Api/Controllers/AuthController.cs` - Endpoints de autenticação
- `backend/src/AssistenteExecutivo.Api/Auth/BffSessionAuthenticationHandler.cs` - Handler de autenticação
- `backend/src/AssistenteExecutivo.Api/Extensions/HttpContextExtensions.cs` - Extensões para obter UserId
- `backend/src/AssistenteExecutivo.Api/Program.cs` - Configuração de sessão e middlewares
- `backend/src/AssistenteExecutivo.Application/Handlers/Auth/GetOwnerUserIdQueryHandler.cs` - Handler para buscar UserId
- `backend/src/AssistenteExecutivo.Api/Security/BffSessionStore.cs` - Armazenamento de dados de sessão

## Tentativas de Correção Já Realizadas

1. **Ajuste de Configuração de Cookies**
   - Mudado `SameSite` de `Lax` para `None`
   - Mudado `SecurePolicy` de `SameAsRequest` para `Always`
   - Aplicado mesmo ao cookie CSRF

2. **Conversão para Client-Side Rendering**
   - Login page convertida para `'use client'`
   - Verificação de autenticação movida para `useEffect`
   - Verificação explícita de cookie `ae.sid` antes de considerar autenticado

3. **Instrumentação com Logs**
   - Logs adicionados em pontos críticos do fluxo
   - Logs no frontend (via fetch para endpoint de ingest)
   - Logs no backend (via `System.IO.File.AppendAllText`)

4. **Verificação Robusta de Cookie**
   - Verificação tanto no header `Cookie` quanto em `Request.Cookies`
   - Endpoint `/auth/session` retorna `authenticated: false` se não houver cookie

## Próximos Passos Sugeridos

1. **Confirmar Reinicialização do Backend**
   - Garantir que o backend foi reiniciado após todas as mudanças
   - Verificar se os logs estão sendo escritos corretamente

2. **Verificar Definição do Cookie**
   - Adicionar logs no OAuth callback para verificar se `Set-Cookie` está sendo enviado
   - Verificar headers da resposta do callback no navegador

3. **Verificar Status do Usuário**
   - Atualizar status do usuário para `Active` no banco de dados
   - Ou implementar verificação de status antes de criar sessão

4. **Verificar Ordem dos Middlewares**
   - Confirmar que `UseSession()` está antes de `UseAuthentication()`
   - Verificar se o `BffSessionAuthenticationHandler` está sendo chamado

5. **Limpar Cache de Sessão**
   - Limpar tabela `SessionCache` no SQL Server
   - Forçar criação de nova sessão após login

6. **Testar com Usuário Ativo**
   - Usar usuário com status `Active` para isolar problema de usuário deletado

## Configuração de Ambiente

- **Domínios**: 
  - Frontend: `assistente.callback-local-cchagas.xyz`
  - Backend: `assistente-api.callback-local-cchagas.xyz`
- **Protocolo**: HTTPS (via tunnel/cloudflared)
- **Banco de Dados**: SQL Server com tabela `SessionCache` para sessões
- **Keycloak**: Realm `assistenteexecutivo`

## Informações Adicionais

- O problema começou com redirects infinitos após login
- Evoluiu para "piscar" da página antes mesmo de chegar à tela de login
- Atualmente, após login, o site não consegue autenticar requisições para endpoints protegidos
- Logs indicam que o cookie `ae.sid` não está sendo enviado nas requisições
- Backend pode não estar executando o código mais recente (sem logs no arquivo de debug)




