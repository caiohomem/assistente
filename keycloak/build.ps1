# Script de build do Keycloak customizado para Windows
# Uso: .\build.ps1 [version] [dockerhub-username]
# Exemplo: .\build.ps1 1.0 caiohb77

$ErrorActionPreference = "Stop"

$VERSION = if ($args[0]) { $args[0] } else { "1.0" }
$DOCKERHUB_USER = if ($args[1]) { $args[1] } else { Read-Host "Docker Hub username" }
$IMAGE_NAME = "keycloak-custom"
$LOCAL_TAG = "${IMAGE_NAME}:${VERSION}"
$DOCKERHUB_TAG = "${DOCKERHUB_USER}/${IMAGE_NAME}:${VERSION}"
$DOCKERHUB_TAG_LATEST = "${DOCKERHUB_USER}/${IMAGE_NAME}:latest"
$TAR_FILE = "${IMAGE_NAME}_${VERSION}.tar"

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Build Keycloak Customizado" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Verifica se Docker está rodando
Write-Host "Verificando Docker..." -ForegroundColor Yellow
try {
    docker info | Out-Null
    Write-Host "✓ Docker está rodando" -ForegroundColor Green
} catch {
    Write-Host "✗ Docker não está rodando. Por favor, inicie o Docker Desktop." -ForegroundColor Red
    exit 1
}

# Build da imagem
Write-Host ""
Write-Host "Construindo imagem Docker..." -ForegroundColor Yellow
Write-Host "Tag: $IMAGE_TAG" -ForegroundColor Gray
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

# Testa localmente (opcional)
$test = Read-Host "Deseja testar a imagem localmente? (S/N)"
if ($test -eq "S" -or $test -eq "s") {
    Write-Host ""
    Write-Host "Iniciando container de teste..." -ForegroundColor Yellow
    Write-Host "Acesse: http://localhost:8080" -ForegroundColor Cyan
    Write-Host "Admin: admin / admin" -ForegroundColor Cyan
    Write-Host "Pressione Ctrl+C para parar" -ForegroundColor Gray
    Write-Host ""
    
    docker run --rm -p 8080:8080 `
        -e KEYCLOAK_ADMIN=admin `
        -e KEYCLOAK_ADMIN_PASSWORD=admin `
        $LOCAL_TAG start-dev
    
    Write-Host ""
    Write-Host "Container de teste finalizado." -ForegroundColor Green
}

# Push para Docker Hub
Write-Host ""
$push = Read-Host "Deseja fazer push para Docker Hub? (S/N)"
if ($push -eq "S" -or $push -eq "s") {
    Write-Host ""
    Write-Host "Verificando login no Docker Hub..." -ForegroundColor Yellow
    
    # Verifica se está logado
    $loginCheck = docker info 2>&1 | Select-String "Username"
    if (-not $loginCheck) {
        Write-Host "Fazendo login no Docker Hub..." -ForegroundColor Yellow
        Write-Host "Use seu username e PAT (Personal Access Token)" -ForegroundColor Cyan
        Write-Host "Para criar um PAT: https://hub.docker.com/settings/security" -ForegroundColor Gray
        Write-Host ""
        
        docker login -u $DOCKERHUB_USER
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "✗ Erro ao fazer login no Docker Hub" -ForegroundColor Red
            exit 1
        }
    } else {
        Write-Host "✓ Já está logado no Docker Hub" -ForegroundColor Green
    }
    
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
    Write-Host "Para usar no Lightsail:" -ForegroundColor Yellow
    Write-Host "  docker pull $DOCKERHUB_TAG" -ForegroundColor White
    Write-Host ""
}

# Exporta para .tar
Write-Host ""
$export = Read-Host "Deseja exportar a imagem para .tar? (S/N)"
if ($export -eq "S" -or $export -eq "s") {
    Write-Host ""
    Write-Host "Exportando imagem para $TAR_FILE..." -ForegroundColor Yellow
    
    docker image save $IMAGE_TAG -o $TAR_FILE
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "✗ Erro ao exportar a imagem" -ForegroundColor Red
        exit 1
    }
    
    $fileSize = (Get-Item $TAR_FILE).Length / 1MB
    Write-Host ""
    Write-Host "✓ Imagem exportada com sucesso!" -ForegroundColor Green
    Write-Host "Arquivo: $TAR_FILE" -ForegroundColor Cyan
    Write-Host "Tamanho: $([math]::Round($fileSize, 2)) MB" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Próximos passos:" -ForegroundColor Yellow
    Write-Host "1. Envie o arquivo para o Lightsail:" -ForegroundColor White
    Write-Host "   scp .\$TAR_FILE ubuntu@SEU_IP:/home/ubuntu/" -ForegroundColor Gray
    Write-Host ""
    Write-Host "2. No Lightsail, importe a imagem:" -ForegroundColor White
    Write-Host "   docker load -i /home/ubuntu/$TAR_FILE" -ForegroundColor Gray
    Write-Host ""
    Write-Host "3. Use o docker-compose.production.yml para subir:" -ForegroundColor White
    Write-Host "   docker compose -f docker-compose.production.yml up -d" -ForegroundColor Gray
}

Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Build concluído!" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Cyan

