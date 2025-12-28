# Script para enviar docker-compose.yml para Lightsail e atualizar
# Uso: .\send-compose-to-lightsail.ps1 [lightsail-ip] [user] [version]
# Exemplo: .\send-compose-to-lightsail.ps1 54.123.45.67 ubuntu 1.0

$ErrorActionPreference = "Stop"

$LIGHTSAIL_IP = if ($args[0]) { $args[0] } else { Read-Host "IP do Lightsail" }
$LIGHTSAIL_USER = if ($args[1]) { $args[1] } else { "ubuntu" }
$VERSION = if ($args[2]) { $args[2] } else { "1.0" }
$COMPOSE_FILE = "docker-compose.production.yml"

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Enviar docker-compose.yml para Lightsail" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Lightsail: ${LIGHTSAIL_USER}@${LIGHTSAIL_IP}" -ForegroundColor Gray
Write-Host "Versão: $VERSION" -ForegroundColor Gray
Write-Host ""

# Verifica se o arquivo existe
if (-not (Test-Path $COMPOSE_FILE)) {
    Write-Host "✗ Arquivo $COMPOSE_FILE não encontrado!" -ForegroundColor Red
    Write-Host "  Certifique-se de estar no diretório keycloak/" -ForegroundColor Yellow
    exit 1
}

Write-Host "Enviando $COMPOSE_FILE para Lightsail..." -ForegroundColor Yellow

# Cria o diretório no Lightsail e envia o arquivo
ssh "${LIGHTSAIL_USER}@${LIGHTSAIL_IP}" "mkdir -p ~/keycloak"

scp $COMPOSE_FILE "${LIGHTSAIL_USER}@${LIGHTSAIL_IP}:~/keycloak/"

if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Erro ao enviar arquivo" -ForegroundColor Red
    exit 1
}

Write-Host "✓ Arquivo enviado com sucesso!" -ForegroundColor Green
Write-Host ""

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Comandos para executar no Lightsail:" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Execute no Lightsail:" -ForegroundColor Yellow
Write-Host ""
Write-Host "cd ~/keycloak" -ForegroundColor White
Write-Host "docker compose -f docker-compose.production.yml down" -ForegroundColor White
Write-Host "docker pull caiohb77/keycloak-custom:$VERSION" -ForegroundColor White
Write-Host "docker compose -f docker-compose.production.yml up -d" -ForegroundColor White
Write-Host "docker logs -f keycloak --tail=200" -ForegroundColor White
Write-Host ""

