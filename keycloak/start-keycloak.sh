#!/bin/bash
set -e

echo "Building Keycloak with PostgreSQL configuration..."
# Força rebuild para garantir que o schema será recriado se necessário
/opt/keycloak/bin/kc.sh build --db=postgres

# Verifica se há arquivos de realm para importar
# O Keycloak importa automaticamente arquivos .json de /opt/keycloak/data/import
# quando iniciado com --import-realm (apenas na primeira vez)
if [ -d "/opt/keycloak/data/import" ] && [ "$(ls -A /opt/keycloak/data/import/*.json 2>/dev/null)" ]; then
    echo "Arquivos de realm encontrados para importação..."
    echo "Iniciando Keycloak com importação de realms..."
    # Importa realms na primeira inicialização
    exec /opt/keycloak/bin/kc.sh start --optimized --import-realm
else
    echo "Nenhum arquivo de realm para importar. Iniciando Keycloak normalmente..."
    exec /opt/keycloak/bin/kc.sh start --optimized
fi

