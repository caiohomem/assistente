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

## Build Local (Windows)

### 1. Construir a imagem

```powershell
cd keycloak
.\build.ps1 [version]
```

Exemplo:
```powershell
.\build.ps1 1.0
```

Isso irá:
- Construir a imagem Docker `keycloak-custom:1.0`
- Opcionalmente testar localmente
- Opcionalmente exportar para `.tar`

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

### 1. Build e exportação

```powershell
.\build.ps1 1.0
# Quando perguntado, escolha exportar para .tar
```

### 2. Enviar para Lightsail

```powershell
.\deploy-to-lightsail.ps1 1.0 [IP_DO_LIGHTSAIL] ubuntu
```

Ou manualmente:

```powershell
scp .\keycloak-custom_1.0.tar ubuntu@SEU_IP:/home/ubuntu/
```

### 3. No Lightsail (SSH)

```bash
# Importar imagem
docker load -i /home/ubuntu/keycloak-custom_1.0.tar

# Verificar
docker images | grep keycloak-custom

# Criar pasta do projeto
mkdir -p ~/keycloak
cd ~/keycloak

# Copiar docker-compose.production.yml
# (use scp ou crie manualmente)

# Subir containers
docker compose -f docker-compose.production.yml up -d

# Ver logs
docker logs -f keycloak --tail=200
```

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

