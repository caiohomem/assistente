# Configuração do Docker Hub

Este guia explica como configurar o Docker Hub para fazer push e pull das imagens do Keycloak customizado.

## 1. Criar Personal Access Token (PAT)

O Docker Hub agora requer um **Personal Access Token (PAT)** em vez de senha para autenticação.

### Passos:

1. Acesse: https://hub.docker.com/settings/security
2. Clique em **"New Access Token"**
3. Dê um nome descritivo (ex: "Keycloak Build")
4. Selecione as permissões:
   - **Read & Write** (para fazer push)
   - Ou apenas **Read** (se só for fazer pull)
5. Clique em **"Generate"**
6. **Copie o token imediatamente** (não será mostrado novamente!)

## 2. Login no Docker Hub

### No Windows (PowerShell):

```powershell
docker login -u SEU_USERNAME
```

Quando solicitado:
- **Username**: seu username do Docker Hub
- **Password**: cole o PAT que você criou

Exemplo:
```powershell
docker login -u caiohb77
# Password: dckr_pat_xxxxxxxxxxxxxxxxxxxxx
```

### Verificar login:

```powershell
docker info | Select-String "Username"
```

## 3. Build e Push

Execute o script de build:

```powershell
cd keycloak
.\build.ps1 1.0 caiohb77
```

Quando perguntado se deseja fazer push, escolha **S**.

O script irá:
1. Construir a imagem
2. Fazer login (se necessário)
3. Fazer push para: `caiohb77/keycloak-custom:1.0`
4. Fazer push para: `caiohb77/keycloak-custom:latest`

## 4. Verificar no Docker Hub

Acesse: https://hub.docker.com/r/SEU_USERNAME/keycloak-custom

Você deve ver as tags publicadas.

## 5. Usar no Lightsail

No Lightsail, faça login e baixe a imagem:

```bash
# Login
docker login -u SEU_USERNAME
# (use o PAT quando solicitado)

# Pull da imagem
docker pull caiohb77/keycloak-custom:1.0

# Verificar
docker images | grep keycloak-custom
```

## 6. Atualizar docker-compose.production.yml

Certifique-se de que o `docker-compose.production.yml` está usando a imagem correta:

```yaml
keycloak:
  image: caiohb77/keycloak-custom:1.0  # Substitua pelo seu username
  # ...
```

## Troubleshooting

### Erro: "unauthorized: authentication required"

- Verifique se está logado: `docker info | grep Username`
- Faça login novamente: `docker login -u SEU_USERNAME`
- Use o PAT, não a senha da conta

### Erro: "denied: requested access to the resource is denied"

- Verifique se o repositório existe no Docker Hub
- Verifique se você tem permissão de escrita
- O repositório será criado automaticamente no primeiro push

### PAT expirado ou perdido

1. Acesse: https://hub.docker.com/settings/security
2. Revogue o token antigo
3. Crie um novo token
4. Faça login novamente

## Segurança

- **Nunca** commite o PAT no código
- **Nunca** compartilhe o PAT publicamente
- Use PATs com escopo mínimo necessário
- Revogue PATs não utilizados
- Considere usar PATs diferentes para diferentes ambientes

## Automação (Opcional)

Para CI/CD, você pode usar variáveis de ambiente:

```powershell
$env:DOCKERHUB_USERNAME = "caiohb77"
$env:DOCKERHUB_TOKEN = "dckr_pat_xxxxxxxxxxxxxxxxxxxxx"

echo $env:DOCKERHUB_TOKEN | docker login -u $env:DOCKERHUB_USERNAME --password-stdin
```

