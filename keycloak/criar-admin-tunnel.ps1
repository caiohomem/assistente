# Script para criar o admin do Keycloak via túnel SSH
# Execute este script e mantenha o terminal aberto

$keyPath = "$env:USERPROFILE\Downloads\lightsail-key.pem"
$LIGHTSAIL_IP = "99.80.217.123"

Write-Host "=== Criando Túnel SSH para Keycloak ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Este script vai criar um túnel SSH para acessar o Keycloak via localhost." -ForegroundColor Yellow
Write-Host "Mantenha este terminal aberto enquanto acessa o Keycloak." -ForegroundColor Yellow
Write-Host ""
Write-Host "Após o túnel ser criado:" -ForegroundColor Green
Write-Host "  1. Abra outro terminal ou navegador" -ForegroundColor White
Write-Host "  2. Acesse: https://localhost:8080" -ForegroundColor White
Write-Host "  3. O Keycloak vai criar o admin automaticamente" -ForegroundColor White
Write-Host ""
Write-Host "Pressione Ctrl+C para encerrar o túnel quando terminar." -ForegroundColor Yellow
Write-Host ""
Write-Host "Criando túnel..." -ForegroundColor Cyan

# Cria o túnel SSH
ssh -L 8080:localhost:8080 -i $keyPath ubuntu@$LIGHTSAIL_IP

