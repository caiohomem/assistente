# Keycloak Customizado - Assistente Executivo

Este diretório contém a configuração do Keycloak com tema customizado embutido na imagem Docker.

## Estrutura

```
keycloak/
  ├── Dockerfile                    # Dockerfile com build stage (Quarkus)
  ├── docker-compose.production.yml # Compose para produção (Lightsail)
  ├── build.ps1                     # Script de build para Windows
  ├── deploy-to-lightsail.ps1      # Script de deploy para Lightsail
  ├── README.md                     # Este arquivo
  └── themes/
      └── assistenteexecutivo/      # Tema customizado
          └── login/
              ├── theme.properties
              ├── login.ftl
              ├── register.ftl
              ├── resources/
              │   ├── css/
              │   ├── js/
              │   └── img/
              └── messages/
```

## Configuração Inicial

### 1. Criar Personal Access Token (PAT) no Docker Hub

O Docker Hub requer um PAT para autenticação. Veja instruções detalhadas em [DOCKERHUB_SETUP.md](DOCKERHUB_SETUP.md).

Resumo rápido:
1. Acesse: https://hub.docker.com/settings/security
2. Crie um novo Access Token
3. Copie o token (não será mostrado novamente!)

### 2. Login no Docker Hub

```powershell
docker login -u SEU_USERNAME
# Use o PAT quando solicitado (não a senha)
```

## Build Local (Windows)

### 1. Construir a imagem e fazer push

```powershell
cd keycloak
.\build.ps1 [version] [dockerhub-username]
```

Exemplo:
```powershell
.\build.ps1 1.0 caiohb77
```

Isso irá:
- Construir a imagem Docker localmente
- Taggar para Docker Hub: `caiohb77/keycloak-custom:1.0`
- Opcionalmente testar localmente
- Opcionalmente fazer push para Docker Hub
- Opcionalmente exportar para `.tar` (se não usar Docker Hub)

### 2. Testar localmente (opcional)

O script `build.ps1` oferece a opção de testar. Ou manualmente:

```powershell
docker run --rm -p 8080:8080 `
  -e KEYCLOAK_ADMIN=admin `
  -e KEYCLOAK_ADMIN_PASSWORD=admin `
  keycloak-custom:1.0 start-dev
```

Acesse: http://localhost:8080

## Deploy para Lightsail

### 1. Build e push para Docker Hub

```powershell
.\build.ps1 1.0 caiohb77
# Quando perguntado, escolha fazer push para Docker Hub (S)
```

### 2. Verificar no Docker Hub

Acesse: https://hub.docker.com/r/caiohb77/keycloak-custom

Certifique-se de que a imagem foi publicada.

### 3. No Lightsail (SSH)

```bash
# Login no Docker Hub
docker login -u caiohb77
# (use o PAT quando solicitado)

# Baixar imagem do Docker Hub
docker pull caiohb77/keycloak-custom:1.0

# Verificar
docker images | grep keycloak-custom

# Criar pasta do projeto
mkdir -p ~/keycloak
cd ~/keycloak

# Copiar docker-compose.production.yml
# (use scp ou crie manualmente)
# IMPORTANTE: Atualize o username no docker-compose.yml

# Subir containers
docker compose -f docker-compose.production.yml up -d

# Ver logs
docker logs -f keycloak --tail=200
```

### 4. Script automatizado (opcional)

```powershell
.\deploy-to-lightsail.ps1 caiohb77 1.0 [IP_DO_LIGHTSAIL] ubuntu
```

Este script fornece instruções passo a passo.

### 4. Acessar e configurar

1. Acesse: `http://SEU_IP:8080`
2. Login: `admin` / `admin`
3. Vá em **Realm Settings → Themes**
4. Configure:
   - **Login theme**: `assistenteexecutivo`
   - **Account theme**: `assistenteexecutivo` (se tiver)
   - **Email theme**: `assistenteexecutivo` (se tiver)
5. Salve

### 5. Verificar tema

```bash
docker exec -it keycloak ls -la /opt/keycloak/themes
```

Deve aparecer `assistenteexecutivo/`.

## Configuração de Produção

### Acesso via IP direto (padrão)

O `docker-compose.production.yml` já está configurado para acesso direto via IP.

### Acesso via domínio/Cloudflare

Edite `docker-compose.production.yml` e descomente/ajuste:

```yaml
KC_HOSTNAME: "auth.seudominio.com"
KC_HOSTNAME_STRICT: "true"
KC_HOSTNAME_STRICT_HTTPS: "true"
KC_HTTP_ENABLED: "false"
KC_HTTPS_PORT: "8443"
KC_PROXY: "edge"
```

### Atualizar imagem do Docker Hub

Edite `docker-compose.production.yml` e atualize o username:

```yaml
keycloak:
  image: SEU_USERNAME/keycloak-custom:1.0  # Substitua SEU_USERNAME
```

## Troubleshooting

### Tema não aparece

1. Verifique se `theme.properties` existe em `themes/assistenteexecutivo/login/`
2. Verifique se o tema foi compilado:
   ```bash
   docker exec -it keycloak ls -la /opt/keycloak/themes
   ```
3. Verifique se está selecionado no Admin Console

### Não conecta

1. Verifique firewall do Lightsail (libere porta 8080)
2. Verifique se o container está rodando:
   ```bash
   docker ps
   ```
3. Verifique logs:
   ```bash
   docker logs keycloak --tail=200
   ```

### Erro de build

1. Verifique se Docker está rodando
2. Verifique se a estrutura de pastas está correta
3. Verifique se a versão do Keycloak está correta no Dockerfile (26.0.0)

## Versões

- **Keycloak**: 26.0.0 (Quarkus)
- **PostgreSQL**: 16
- **Tema**: assistenteexecutivo

## Notas

- O tema é **embutido na imagem** durante o build, não precisa de volumes
- O build usa **multi-stage** para otimizar o tamanho da imagem
- A imagem final contém apenas o runtime necessário

