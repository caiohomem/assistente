$ErrorActionPreference = "Stop"

Write-Host "Rebuilding Keycloak image (with embedded theme)..." -ForegroundColor Cyan
docker compose -f "$PSScriptRoot/docker-compose.keycloak.yml" build keycloak

Write-Host "Restarting Keycloak container (keeping volumes/db)..." -ForegroundColor Cyan
docker compose -f "$PSScriptRoot/docker-compose.keycloak.yml" up -d keycloak

Write-Host "Done. If Keycloak was already running, give it ~10-30s to start." -ForegroundColor Green
Write-Host "Tip: check the theme in the login page or via Admin Console Realm Settings -> Themes." -ForegroundColor Green

