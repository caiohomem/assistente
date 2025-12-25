# Script para zerar e reiniciar o Keycloak
# Execute: .\reset-keycloak.ps1
# ou: cd docker && .\reset-keycloak.ps1

$composeFile = "docker-compose.keycloak.yml"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$composePath = Join-Path $scriptDir $composeFile

# Verificar se o arquivo existe
if (-not (Test-Path $composePath)) {
    Write-Host "✗ Erro: Arquivo $composeFile não encontrado em $scriptDir" -ForegroundColor Red
    exit 1
}

# Detectar se está usando SQL Server ou H2 (memory)
Write-Host "[0/4] Detectando configuração do banco de dados..." -ForegroundColor Yellow
$composeLines = Get-Content $composePath
# Verifica se há uma linha não comentada com KC_DB: mssql
$usingSql = $false
foreach ($line in $composeLines) {
    $trimmed = $line.Trim()
    if ($trimmed -match "^KC_DB:\s*mssql" -and $trimmed -notmatch "^#") {
        $usingSql = $true
        break
    }
}

if ($usingSql) {
    Write-Host "✓ Configuração detectada: SQL Server" -ForegroundColor Cyan
    $dbType = "SQL Server"
} else {
    Write-Host "✓ Configuração detectada: H2 In-Memory" -ForegroundColor Cyan
    $dbType = "H2 In-Memory"
}

Write-Host ""
Write-Host "=== Reset do Keycloak ($dbType) ===" -ForegroundColor Cyan
Write-Host ""

# Parar e remover containers E volumes
Write-Host "[1/4] Parando containers e removendo volumes..." -ForegroundColor Yellow
docker-compose -f $composePath down -v

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Containers parados e volumes removidos" -ForegroundColor Green
} else {
    Write-Host "⚠ Aviso: Algum erro ao parar (pode ser que não estava rodando)" -ForegroundColor Yellow
}

# Forçar remoção do container caso ainda exista
Write-Host "Removendo container 'keycloak' se existir..." -ForegroundColor Yellow
docker rm -f keycloak 2>$null | Out-Null

# Listar e remover volumes relacionados ao keycloak (caso ainda existam)
Write-Host ""
Write-Host "[2/4] Verificando e removendo volumes órfãos..." -ForegroundColor Yellow
$volumes = docker volume ls -q | Where-Object { $_ -like "*keycloak*" }
if ($volumes) {
    foreach ($vol in $volumes) {
        Write-Host "  Removendo volume: $vol" -ForegroundColor Gray
        docker volume rm $vol 2>$null | Out-Null
    }
    Write-Host "✓ Volumes órfãos removidos" -ForegroundColor Green
} else {
    Write-Host "✓ Nenhum volume órfão encontrado" -ForegroundColor Green
}

Write-Host ""
Write-Host "[3/4] Aguardando 2 segundos..." -ForegroundColor Yellow
Start-Sleep -Seconds 2

Write-Host ""
Write-Host "[4/4] Reiniciando Keycloak com banco limpo..." -ForegroundColor Yellow
docker-compose -f $composePath up -d

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "=== Keycloak Reiniciado com Sucesso! ===" -ForegroundColor Green
    Write-Host ""
    Write-Host "Acesse: http://localhost:8080" -ForegroundColor Cyan
    Write-Host "Login: admin / admin" -ForegroundColor Cyan
    Write-Host ""
    if ($usingSql) {
        Write-Host "⚠ ATENÇÃO: Usando SQL Server - os dados do banco NÃO foram removidos!" -ForegroundColor Yellow
        Write-Host "  Para limpar completamente, você precisa:" -ForegroundColor Yellow
        Write-Host "  1. Conectar ao SQL Server e limpar o banco 'keycloak'" -ForegroundColor White
        Write-Host "  2. Ou executar: DROP DATABASE keycloak; CREATE DATABASE keycloak;" -ForegroundColor White
        Write-Host ""
    } else {
        Write-Host "✓ Banco H2 In-Memory - dados serão perdidos ao reiniciar (comportamento esperado)" -ForegroundColor Green
        Write-Host ""
    }
    Write-Host "Aguarde alguns segundos para o Keycloak inicializar completamente." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Para ver os logs:" -ForegroundColor Yellow
    Write-Host "  docker-compose -f $composeFile logs -f" -ForegroundColor White
    Write-Host ""
    Write-Host "Para verificar se está pronto:" -ForegroundColor Yellow
    Write-Host "  docker-compose -f $composeFile logs | Select-String 'started in'" -ForegroundColor White
} else {
    Write-Host ""
    Write-Host "✗ Erro ao reiniciar o Keycloak" -ForegroundColor Red
    Write-Host "Verifique os logs:" -ForegroundColor Yellow
    Write-Host "  docker-compose -f $composeFile logs" -ForegroundColor White
}

