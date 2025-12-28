# Script de deploy para Lightsail
# Uso: .\deploy-to-lightsail.ps1 [version] [lightsail-ip] [user]
# Exemplo: .\deploy-to-lightsail.ps1 1.0 54.123.45.67 ubuntu

$ErrorActionPreference = "Stop"

$VERSION = if ($args[0]) { $args[0] } else { "1.0" }
$LIGHTSAIL_IP = if ($args[1]) { $args[1] } else { Read-Host "IP do Lightsail" }
$LIGHTSAIL_USER = if ($args[2]) { $args[2] } else { "ubuntu" }
$IMAGE_NAME = "keycloak-custom"
$IMAGE_TAG = "${IMAGE_NAME}:${VERSION}"
$TAR_FILE = "${IMAGE_NAME}_${VERSION}.tar"

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Deploy para Lightsail" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Verifica se o arquivo .tar existe
if (-not (Test-Path $TAR_FILE)) {
    Write-Host "✗ Arquivo $TAR_FILE não encontrado!" -ForegroundColor Red
    Write-Host "Execute primeiro: .\build.ps1 $VERSION" -ForegroundColor Yellow
    exit 1
}

Write-Host "Arquivo encontrado: $TAR_FILE" -ForegroundColor Green
$fileSize = (Get-Item $TAR_FILE).Length / 1MB
Write-Host "Tamanho: $([math]::Round($fileSize, 2)) MB" -ForegroundColor Gray
Write-Host ""

# Envia para Lightsail
Write-Host "Enviando arquivo para Lightsail..." -ForegroundColor Yellow
Write-Host "IP: $LIGHTSAIL_IP" -ForegroundColor Gray
Write-Host "Usuário: $LIGHTSAIL_USER" -ForegroundColor Gray
Write-Host ""

scp $TAR_FILE "${LIGHTSAIL_USER}@${LIGHTSAIL_IP}:/home/${LIGHTSAIL_USER}/"

if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Erro ao enviar arquivo. Verifique:" -ForegroundColor Red
    Write-Host "  - IP do Lightsail está correto" -ForegroundColor Yellow
    Write-Host "  - SSH está configurado (chave .pem)" -ForegroundColor Yellow
    Write-Host "  - Firewall permite conexão SSH" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "✓ Arquivo enviado com sucesso!" -ForegroundColor Green
Write-Host ""

# Instruções para o Lightsail
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Próximos passos no Lightsail:" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Conecte-se ao Lightsail:" -ForegroundColor Yellow
Write-Host "   ssh ${LIGHTSAIL_USER}@${LIGHTSAIL_IP}" -ForegroundColor White
Write-Host ""
Write-Host "2. Importe a imagem Docker:" -ForegroundColor Yellow
Write-Host "   docker load -i /home/${LIGHTSAIL_USER}/$TAR_FILE" -ForegroundColor White
Write-Host ""
Write-Host "3. Verifique se a imagem foi importada:" -ForegroundColor Yellow
Write-Host "   docker images | grep keycloak-custom" -ForegroundColor White
Write-Host ""
Write-Host "4. Crie a pasta do projeto (se não existir):" -ForegroundColor Yellow
Write-Host "   mkdir -p ~/keycloak" -ForegroundColor White
Write-Host "   cd ~/keycloak" -ForegroundColor White
Write-Host ""
Write-Host "5. Copie o docker-compose.production.yml para o Lightsail:" -ForegroundColor Yellow
Write-Host "   (ou crie manualmente com o conteúdo do arquivo)" -ForegroundColor Gray
Write-Host ""
Write-Host "6. Suba os containers:" -ForegroundColor Yellow
Write-Host "   docker compose -f docker-compose.production.yml up -d" -ForegroundColor White
Write-Host ""
Write-Host "7. Verifique os logs:" -ForegroundColor Yellow
Write-Host "   docker logs -f keycloak --tail=200" -ForegroundColor White
Write-Host ""
Write-Host "8. Acesse o Keycloak:" -ForegroundColor Yellow
Write-Host "   http://${LIGHTSAIL_IP}:8080" -ForegroundColor White
Write-Host ""
Write-Host "9. Configure o tema no Admin Console:" -ForegroundColor Yellow
Write-Host "   Realm Settings → Themes → Login theme: assistenteexecutivo" -ForegroundColor White
Write-Host ""

