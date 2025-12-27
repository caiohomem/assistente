# Theme Assistente Executivo para Keycloak

Este theme customiza as páginas de login e registro do Keycloak para parecerem parte do sistema Assistente Executivo.

## Estrutura

```
assistenteexecutivo/
├── login/
│   ├── theme.properties          # Configuração do theme
│   ├── login.ftl                 # Template de login
│   ├── register.ftl              # Template de registro
│   ├── resources/
│   │   ├── css/
│   │   │   └── login.css        # Estilos customizados
│   │   ├── img/
│   │   │   └── logo.png          # Logo da aplicação (adicionar)
│   │   └── js/
│   │       └── login.js          # JavaScript customizado (opcional)
│   └── messages/
│       └── messages_pt_BR.properties  # Traduções em português
└── account/                      # Páginas de gerenciamento de conta (futuro)
```

## Instalação

### Desenvolvimento (Docker)

Adicionar volume no `docker-compose.yml`:

```yaml
services:
  keycloak:
    volumes:
      - ./keycloak/themes/assistenteexecutivo:/opt/keycloak/themes/assistenteexecutivo
```

### Produção

1. Copiar a pasta `assistenteexecutivo` para `/opt/keycloak/themes/` no container/servidor Keycloak
2. Reiniciar o Keycloak
3. O theme será configurado automaticamente via `ConfigureRealmThemeAsync` no provisionamento

## Configuração

O theme é configurado automaticamente quando um realm é criado via `KeycloakService.CreateRealmAsync()`.

Para configurar manualmente:
1. Acesse Keycloak Admin Console
2. Vá em Realm Settings > Themes
3. Selecione "assistenteexecutivo" em:
   - Login theme
   - Account theme
   - Email theme

## Customização

### Cores

As cores podem ser ajustadas em `login/resources/css/login.css`:

```css
:root {
    --primary-color: #4F46E5; /* indigo-600 */
    --primary-hover: #4338CA; /* indigo-700 */
    --primary-light: #6366F1; /* indigo-500 */
}
```

### Logo

Adicionar logo em `login/resources/img/logo.png` (recomendado: 200px width, PNG com transparência).

### Traduções

Editar `login/messages/messages_pt_BR.properties` para customizar textos.

## Suporte a Dark Mode

O theme suporta dark mode automaticamente quando o Keycloak detecta a preferência do sistema ou quando a classe `.dark` é aplicada no HTML.

## Notas

- O theme herda de `keycloak` (theme padrão)
- Templates usam Freemarker syntax
- CSS é carregado automaticamente pelo Keycloak
- Mudanças requerem restart do Keycloak para serem aplicadas




