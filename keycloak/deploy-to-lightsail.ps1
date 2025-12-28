# Script de deploy para Lightsail (usando Docker Hub)
# Uso: .\deploy-to-lightsail.ps1 [dockerhub-username] [version] [lightsail-ip] [user]
# Exemplo: .\deploy-to-lightsail.ps1 caiohb77 1.0 54.123.45.67 ubuntu

$ErrorActionPreference = "Stop"

$DOCKERHUB_USER = if ($args[0]) { $args[0] } else { Read-Host "Docker Hub username" }
$VERSION = if ($args[1]) { $args[1] } else { "1.0" }
$LIGHTSAIL_IP = if ($args[2]) { $args[2] } else { Read-Host "IP do Lightsail" }
$LIGHTSAIL_USER = if ($args[3]) { $args[3] } else { "ubuntu" }
$IMAGE_NAME = "keycloak-custom"
$DOCKERHUB_TAG = "${DOCKERHUB_USER}/${IMAGE_NAME}:${VERSION}"

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Deploy para Lightsail (via Docker Hub)" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Docker Hub: $DOCKERHUB_TAG" -ForegroundColor Gray
Write-Host "Lightsail: ${LIGHTSAIL_USER}@${LIGHTSAIL_IP}" -ForegroundColor Gray
Write-Host ""

# Verifica se a imagem existe no Docker Hub (opcional)
Write-Host "Verificando se a imagem existe no Docker Hub..." -ForegroundColor Yellow
$check = docker manifest inspect $DOCKERHUB_TAG 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "⚠ Aviso: Não foi possível verificar a imagem no Docker Hub" -ForegroundColor Yellow
    Write-Host "  Certifique-se de que a imagem foi publicada:" -ForegroundColor Yellow
    Write-Host "  https://hub.docker.com/r/${DOCKERHUB_USER}/${IMAGE_NAME}" -ForegroundColor White
    Write-Host ""
    $continue = Read-Host "Deseja continuar mesmo assim? (S/N)"
    if ($continue -ne "S" -and $continue -ne "s") {
        exit 0
    }
} else {
    Write-Host "✓ Imagem encontrada no Docker Hub" -ForegroundColor Green
}

Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Próximos passos no Lightsail:" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Conecte-se ao Lightsail:" -ForegroundColor Yellow
Write-Host "   ssh ${LIGHTSAIL_USER}@${LIGHTSAIL_IP}" -ForegroundColor White
Write-Host ""
Write-Host "2. Faça login no Docker Hub (se necessário):" -ForegroundColor Yellow
Write-Host "   docker login -u ${DOCKERHUB_USER}" -ForegroundColor White
Write-Host "   (Use seu PAT - Personal Access Token)" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Baixe a imagem do Docker Hub:" -ForegroundColor Yellow
Write-Host "   docker pull $DOCKERHUB_TAG" -ForegroundColor White
Write-Host ""
Write-Host "4. Verifique se a imagem foi baixada:" -ForegroundColor Yellow
Write-Host "   docker images | grep keycloak-custom" -ForegroundColor White
Write-Host ""
Write-Host "5. Crie a pasta do projeto (se não existir):" -ForegroundColor Yellow
Write-Host "   mkdir -p ~/keycloak" -ForegroundColor White
Write-Host "   cd ~/keycloak" -ForegroundColor White
Write-Host ""
Write-Host "6. Copie o docker-compose.production.yml para o Lightsail:" -ForegroundColor Yellow
Write-Host "   (use scp ou crie manualmente)" -ForegroundColor Gray
Write-Host "   Certifique-se de atualizar o username no docker-compose:" -ForegroundColor Gray
Write-Host "   image: ${DOCKERHUB_USER}/${IMAGE_NAME}:${VERSION}" -ForegroundColor White
Write-Host ""
Write-Host "7. Suba os containers:" -ForegroundColor Yellow
Write-Host "   docker compose -f docker-compose.production.yml up -d" -ForegroundColor White
Write-Host ""
Write-Host "8. Verifique os logs:" -ForegroundColor Yellow
Write-Host "   docker logs -f keycloak --tail=200" -ForegroundColor White
Write-Host ""
Write-Host "9. Acesse o Keycloak:" -ForegroundColor Yellow
Write-Host "   http://${LIGHTSAIL_IP}:8080" -ForegroundColor White
Write-Host ""
Write-Host "10. Configure o tema no Admin Console:" -ForegroundColor Yellow
Write-Host "    Realm Settings → Themes → Login theme: assistenteexecutivo" -ForegroundColor White
Write-Host ""
