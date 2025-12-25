# AssistenteExecutivo (Flutter Mobile)

Este app faz login no **Keycloak** via **OAuth2 Authorization Code + PKCE** e chama um endpoint protegido na API.

## Pré-requisitos

- Flutter instalado e no PATH (`flutter doctor`)
- API rodando (DEV): `backend/src/AssistenteExecutivo.Api` em `http://localhost:5239`
- Keycloak rodando (DEV): padrão `http://localhost:8080` (realm `assistenteexecutivo`)

## Configuração (URLs)

O app usa `--dart-define` (com defaults pensados para emulador Android):

- `API_BASE_URL`
  - Android emulator: `http://10.0.2.2:5239`
  - iOS simulator: `http://localhost:5239`
- `KEYCLOAK_BASE_URL`
  - **Para Google OAuth funcionar**: use a URL pública `https://auth.callback-local-cchagas.xyz`
  - Android emulator (sem Google): `http://10.0.2.2:8080` (default)
  - iOS simulator: `http://localhost:8080` (default)
- `KEYCLOAK_REALM` (default: `assistenteexecutivo`)
- `KEYCLOAK_CLIENT_ID` (default: `assistenteexecutivo-app`)

### Usando Keycloak público (recomendado para Google OAuth)

Se o Keycloak está exposto publicamente (ex.: `https://auth.callback-local-cchagas.xyz/`), use:

```bash
flutter run --dart-define=KEYCLOAK_BASE_URL=https://auth.callback-local-cchagas.xyz
```

Isso resolve o erro do Google OAuth bloqueando IPs privados.

## Redirect URI (PKCE)

O app usa:

- **redirectUri**: `com.assistenteexecutivo.app:/oauthredirect`
- **scheme**: `com.assistenteexecutivo.app`

> Observação: o provisionamento DEV do Keycloak cria/atualiza o client com `redirectUris = ["*"]`, então esse redirect deve funcionar.

## Rodar

Dentro de `app/`:

**Opção 1: Keycloak público (recomendado para Google OAuth)**
```bash
flutter pub get
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5239 --dart-define=KEYCLOAK_BASE_URL=https://auth.callback-local-cchagas.xyz
```

**Opção 2: Keycloak local (apenas login direto, sem Google OAuth)**
```bash
flutter pub get
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5239 --dart-define=KEYCLOAK_BASE_URL=http://10.0.2.2:8080
```

## Teste do endpoint protegido

O backend expõe:

- `GET /api/me` (Bearer token do Keycloak)

No app:

- **Login (PKCE)**
- **Call /api/me**

## BFF (CSRF + cookies) — compatível com o webapp

O webapp usa o padrão:

- `GET /auth/session` para obter **csrfToken** e receber cookies (`ae.sid`, `XSRF-TOKEN`)
- Envia `X-CSRF-TOKEN: <csrfToken>` + `Cookie: ...` em POSTs

No mobile, foi adicionado um bloco **BFF (cookies + CSRF)** com:

- **Consultar /auth/session**
- **Esqueci senha** (`POST /auth/forgot-password`)
- **Reset senha** (`POST /auth/reset-password`)
- **Logout (BFF)** (`POST /auth/logout`)

## Usuários DEV (KeycloakAdminProvisioner)

Se o provisionamento do Keycloak estiver habilitado no backend (DEV), estes usuários são criados:

- `admin@assistenteexecutivo.local` / `Admin@123`
- `user@assistenteexecutivo.local` / `User@123`
- `viewer@assistenteexecutivo.local` / `Viewer@123`

## Troubleshooting

### "No stored state - unable to handle response"

Esse erro acontece quando o `flutter_appauth` perde o state durante o redirect do Keycloak. Causas comuns:

1. **Hot Reload/Restart durante o login**: Não faça hot reload/restart enquanto o browser do Keycloak estiver aberto.
2. **Processo do app morto**: O Android pode matar o processo quando o browser abre. Tente:
   - Desativar "Don't keep activities" nas Developer Options do emulador
   - Garantir que o app está em foreground antes de iniciar o login
3. **State não persistido**: O `flutter_appauth` armazena state em memória/SharedPreferences. Se o processo for recriado, o state se perde.

**Solução**: Tente o login novamente sem fazer hot reload durante o processo. Se persistir, pode ser necessário fazer um rebuild completo (`flutter clean && flutter run`).

### "Access blocked: Authorization Error" (Google OAuth)

Se você ver um erro do Google dizendo que `device_id` e `device_name` são necessários para IP privado, isso acontece porque:

1. O Keycloak está redirecionando para o Google IdP
2. O Google bloqueia requisições de IPs privados (como `10.0.2.2` do emulador) sem esses parâmetros

**Solução**: Use a URL pública do Keycloak (veja seção "Usando Keycloak público" acima).

### "No stored state" após login com Google

Esse erro é comum quando você faz login via Google OAuth. O fluxo longo (Google → Keycloak → App) pode fazer o Android recriar o processo do app, perdendo o state do AppAuth.

**O que acontece**:
1. App inicia login → AppAuth armazena state
2. Browser abre → Google OAuth
3. Google redireciona → Keycloak
4. Keycloak redireciona → App (deep link)
5. **Problema**: Android pode ter recriado o processo, perdendo o state

**Soluções**:

1. **Retry automático**: O app tenta automaticamente 2 vezes. Se ainda falhar:
2. **Use login direto no Keycloak** (mais confiável para dev):
   - No Keycloak Admin Console, vá em **Identity Providers → google**
   - Desabilite temporariamente o Google IdP (Enabled: OFF)
   - Use login direto com username/password (ex.: `admin@assistenteexecutivo.local` / `Admin@123`)
   - Fluxo mais curto = menos chance de perder state

3. **Evite hot reload durante login**: Não faça hot reload/restart enquanto o browser estiver aberto.

4. **Desative "Don't keep activities"**: Nas Developer Options do emulador, desative essa opção para evitar que o Android mate o processo.


