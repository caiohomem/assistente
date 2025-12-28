# Script de build e deploy do Keycloak customizado para Windows
# Uso: .\build.ps1 [version] [dockerhub-username] [lightsail-ip] [ssh-key-path]
# Exemplo: .\build.ps1 1.0 caiohb77 99.80.217.123

$ErrorActionPreference = "Stop"

# Configuração do Docker Hub
$DOCKERHUB_PAT = "dckr_pat_zRwEd2CwHycXn1ctscswlNG1kw4"  # Substitua pelo seu Personal Access Token

# Configuração do Lightsail
$LIGHTSAIL_IP = if ($args[2]) { $args[2] } else { "99.80.217.123" }
$LIGHTSAIL_USER = "ubuntu"
$SSH_KEY_PATH = if ($args[3]) { $args[3] } else { "$env:USERPROFILE\Downloads\lightsail-key.pem" }

$VERSION = if ($args[0]) { $args[0] } else { "1.0" }
$DOCKERHUB_USER = if ($args[1]) { $args[1] } else { Read-Host "Docker Hub username" }
$IMAGE_NAME = "keycloak-custom"
$LOCAL_TAG = "${IMAGE_NAME}:${VERSION}"
$DOCKERHUB_TAG = "${DOCKERHUB_USER}/${IMAGE_NAME}:${VERSION}"
$DOCKERHUB_TAG_LATEST = "${DOCKERHUB_USER}/${IMAGE_NAME}:latest"
$COMPOSE_FILE = "docker-compose.production.yml"

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Build Keycloak Customizado" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Verifica se Docker está rodando
Write-Host "Verificando Docker..." -ForegroundColor Yellow
$dockerCheck = docker info 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Docker não está rodando. Por favor, inicie o Docker Desktop." -ForegroundColor Red
    Write-Host "  Erro: $dockerCheck" -ForegroundColor Gray
    exit 1
}
Write-Host "✓ Docker está rodando" -ForegroundColor Green

# Build da imagem
Write-Host ""
Write-Host "Construindo imagem Docker..." -ForegroundColor Yellow
Write-Host "  Local: $LOCAL_TAG" -ForegroundColor Gray
Write-Host "  Docker Hub: $DOCKERHUB_TAG" -ForegroundColor Gray
Write-Host ""

docker build -t $LOCAL_TAG -t $DOCKERHUB_TAG -t $DOCKERHUB_TAG_LATEST .

if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Erro ao construir a imagem" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "✓ Imagem construída com sucesso!" -ForegroundColor Green
Write-Host "  Local: $LOCAL_TAG" -ForegroundColor Gray
Write-Host "  Docker Hub: $DOCKERHUB_TAG" -ForegroundColor Gray
Write-Host ""

# Push para Docker Hub
Write-Host ""
Write-Host "Fazendo login no Docker Hub..." -ForegroundColor Yellow

# Login usando PAT fixo
$DOCKERHUB_PAT | docker login -u $DOCKERHUB_USER --password-stdin

if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Erro ao fazer login no Docker Hub" -ForegroundColor Red
    Write-Host "  Verifique se o PAT está correto no script" -ForegroundColor Yellow
    exit 1
}

Write-Host "✓ Login realizado com sucesso" -ForegroundColor Green
    
Write-Host ""
Write-Host "Fazendo push para Docker Hub..." -ForegroundColor Yellow
Write-Host "Tag: $DOCKERHUB_TAG" -ForegroundColor Gray
Write-Host "Tag (latest): $DOCKERHUB_TAG_LATEST" -ForegroundColor Gray
Write-Host ""

docker push $DOCKERHUB_TAG
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Erro ao fazer push da versão $VERSION" -ForegroundColor Red
    exit 1
}

docker push $DOCKERHUB_TAG_LATEST
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Erro ao fazer push da tag latest" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "✓ Push concluído com sucesso!" -ForegroundColor Green
Write-Host ""
Write-Host "Imagem disponível em:" -ForegroundColor Cyan
Write-Host "  https://hub.docker.com/r/${DOCKERHUB_USER}/${IMAGE_NAME}" -ForegroundColor White
Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Deploy para Lightsail" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Verifica se a chave SSH existe
if (-not (Test-Path $SSH_KEY_PATH)) {
    Write-Host "⚠ Chave SSH não encontrada: $SSH_KEY_PATH" -ForegroundColor Yellow
    Write-Host "  Pulando deploy automático. Execute manualmente:" -ForegroundColor Yellow
    Write-Host "  scp -i CAMINHO_CHAVE.pem $COMPOSE_FILE ${LIGHTSAIL_USER}@${LIGHTSAIL_IP}:~/keycloak/" -ForegroundColor Gray
    Write-Host ""
    exit 0
}

# Corrige permissões da chave SSH (se necessário)
Write-Host "Verificando permissões da chave SSH..." -ForegroundColor Yellow
$keyAcl = icacls $SSH_KEY_PATH 2>&1 | Select-String "$env:USERNAME.*:\(F\)"
if (-not $keyAcl) {
    Write-Host "Corrigindo permissões da chave SSH..." -ForegroundColor Yellow
    icacls $SSH_KEY_PATH /inheritance:r | Out-Null
    icacls $SSH_KEY_PATH /grant:r "${env:USERNAME}:R" | Out-Null
}

# Cria diretório no Lightsail
Write-Host "Criando diretório no Lightsail..." -ForegroundColor Yellow
ssh -i $SSH_KEY_PATH -o StrictHostKeyChecking=no "${LIGHTSAIL_USER}@${LIGHTSAIL_IP}" "mkdir -p ~/keycloak" 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "⚠ Aviso ao criar diretório (pode já existir)" -ForegroundColor Yellow
}

# Envia docker-compose.yml
Write-Host "Enviando docker-compose.yml para Lightsail..." -ForegroundColor Yellow
$composePath = Join-Path $PSScriptRoot $COMPOSE_FILE
if (-not (Test-Path $composePath)) {
    Write-Host "✗ Arquivo $COMPOSE_FILE não encontrado em $PSScriptRoot" -ForegroundColor Red
    exit 1
}

scp -i $SSH_KEY_PATH -o StrictHostKeyChecking=no $composePath "${LIGHTSAIL_USER}@${LIGHTSAIL_IP}:~/keycloak/" 2>&1 | Out-Null

if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Erro ao enviar docker-compose.yml" -ForegroundColor Red
    Write-Host "  Execute manualmente:" -ForegroundColor Yellow
    Write-Host "  scp -i $SSH_KEY_PATH $composePath ${LIGHTSAIL_USER}@${LIGHTSAIL_IP}:~/keycloak/" -ForegroundColor Gray
    exit 1
}

Write-Host "✓ Arquivo enviado com sucesso" -ForegroundColor Green

# Verifica e instala docker-compose se necessário
Write-Host ""
Write-Host "Verificando docker-compose..." -ForegroundColor Yellow
$checkCompose = ssh -i $SSH_KEY_PATH -o StrictHostKeyChecking=no "${LIGHTSAIL_USER}@${LIGHTSAIL_IP}" "which docker-compose > /dev/null 2>&1 && echo 'installed' || (docker compose version > /dev/null 2>&1 && echo 'plugin' || echo 'not-found')" 2>&1
$composeStatus = $checkCompose | Select-String -Pattern "installed|plugin|not-found" | ForEach-Object { $_.Line.Trim() }

if ($composeStatus -eq "not-found" -or -not $composeStatus) {
    Write-Host "⚠ docker-compose não encontrado. Instalando..." -ForegroundColor Yellow
    
    # Instala docker-compose
    $installCmd = @"
sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose && \
sudo chmod +x /usr/local/bin/docker-compose && \
docker-compose --version
"@
    
    $installOutput = ssh -i $SSH_KEY_PATH -o StrictHostKeyChecking=no "${LIGHTSAIL_USER}@${LIGHTSAIL_IP}" $installCmd 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "✗ Erro ao instalar docker-compose" -ForegroundColor Red
        Write-Host "  Saída: $installOutput" -ForegroundColor Gray
        Write-Host ""
        Write-Host "Instale manualmente no Lightsail:" -ForegroundColor Yellow
        Write-Host "  sudo curl -L 'https://github.com/docker/compose/releases/latest/download/docker-compose-linux-x86_64' -o /usr/local/bin/docker-compose" -ForegroundColor White
        Write-Host "  sudo chmod +x /usr/local/bin/docker-compose" -ForegroundColor White
        exit 1
    }
    
    Write-Host "✓ docker-compose instalado com sucesso" -ForegroundColor Green
    $DOCKER_COMPOSE_CMD = "docker-compose"
} elseif ($composeStatus -eq "plugin") {
    Write-Host "✓ docker compose (plugin) encontrado" -ForegroundColor Green
    $DOCKER_COMPOSE_CMD = "docker compose"
} else {
    Write-Host "✓ docker-compose encontrado" -ForegroundColor Green
    $DOCKER_COMPOSE_CMD = "docker-compose"
}

$downCmd = "cd ~/keycloak && (sudo $DOCKER_COMPOSE_CMD -f docker-compose.production.yml down 2>&1 || $DOCKER_COMPOSE_CMD -f docker-compose.production.yml down 2>&1 || true)"
ssh -i $SSH_KEY_PATH -o StrictHostKeyChecking=no "${LIGHTSAIL_USER}@${LIGHTSAIL_IP}" $downCmd | Out-Null

# Adiciona usuário ao grupo docker (se necessário)
Write-Host "Verificando permissões do Docker..." -ForegroundColor Yellow
$dockerGroupCmd = "sudo usermod -aG docker $USER 2>&1 || true"
ssh -i $SSH_KEY_PATH -o StrictHostKeyChecking=no "${LIGHTSAIL_USER}@${LIGHTSAIL_IP}" $dockerGroupCmd | Out-Null

# Faz pull da nova imagem
Write-Host "Baixando nova imagem do Docker Hub..." -ForegroundColor Yellow
$pullCmd = "cd ~/keycloak && sudo docker pull $DOCKERHUB_TAG 2>&1 || docker pull $DOCKERHUB_TAG 2>&1"
$pullOutput = ssh -i $SSH_KEY_PATH -o StrictHostKeyChecking=no "${LIGHTSAIL_USER}@${LIGHTSAIL_IP}" $pullCmd 2>&1

if ($LASTEXITCODE -ne 0 -or $pullOutput -match "manifest unknown|not found") {
    Write-Host "⚠ Erro: Imagem não encontrada no Docker Hub" -ForegroundColor Yellow
    Write-Host "  Verifique se a imagem foi publicada: https://hub.docker.com/r/${DOCKERHUB_USER}/${IMAGE_NAME}/tags" -ForegroundColor Gray
    Write-Host "  Ou faça login no Docker Hub: docker login -u $DOCKERHUB_USER" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  Saída: $pullOutput" -ForegroundColor Gray
} else {
    Write-Host "✓ Imagem baixada com sucesso" -ForegroundColor Green
}

# Sobe os containers
Write-Host ""
Write-Host "Subindo containers..." -ForegroundColor Yellow
$upCmd = "cd ~/keycloak && (sudo $DOCKER_COMPOSE_CMD -f docker-compose.production.yml up -d 2>&1 || $DOCKER_COMPOSE_CMD -f docker-compose.production.yml up -d 2>&1)"
$upOutput = ssh -i $SSH_KEY_PATH -o StrictHostKeyChecking=no "${LIGHTSAIL_USER}@${LIGHTSAIL_IP}" $upCmd 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Erro ao subir containers" -ForegroundColor Red
    Write-Host "  Saída: $upOutput" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Execute manualmente no Lightsail:" -ForegroundColor Yellow
    Write-Host "  cd ~/keycloak" -ForegroundColor White
    Write-Host "  $DOCKER_COMPOSE_CMD -f docker-compose.production.yml up -d" -ForegroundColor White
    exit 1
}

Write-Host "✓ Containers iniciados com sucesso" -ForegroundColor Green

# Aguarda um pouco e verifica logs
Write-Host ""
Write-Host "Aguardando inicialização (5 segundos)..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

Write-Host ""
Write-Host "Verificando status dos containers..." -ForegroundColor Yellow
$statusCmd = "cd ~/keycloak && (sudo $DOCKER_COMPOSE_CMD -f docker-compose.production.yml ps 2>&1 || $DOCKER_COMPOSE_CMD -f docker-compose.production.yml ps 2>&1 || docker ps --filter name=keycloak 2>&1)"
$status = ssh -i $SSH_KEY_PATH -o StrictHostKeyChecking=no "${LIGHTSAIL_USER}@${LIGHTSAIL_IP}" $statusCmd 2>&1
Write-Host $status

Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Deploy concluído!" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Keycloak disponível em:" -ForegroundColor Cyan
Write-Host "  http://${LIGHTSAIL_IP}:8080" -ForegroundColor White
Write-Host ""
Write-Host "Para ver os logs:" -ForegroundColor Yellow
Write-Host "  ssh -i $SSH_KEY_PATH ${LIGHTSAIL_USER}@${LIGHTSAIL_IP} 'docker logs -f keycloak --tail=200'" -ForegroundColor Gray
Write-Host ""

