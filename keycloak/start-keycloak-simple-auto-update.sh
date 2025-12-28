#!/bin/bash
set -e

echo "Building Keycloak with PostgreSQL configuration..."
/opt/keycloak/bin/kc.sh build --db=postgres

# Função para obter token de admin
get_admin_token() {
    local response=$(curl -s -X POST "http://localhost:8080/realms/master/protocol/openid-connect/token" \
        -H "Content-Type: application/x-www-form-urlencoded" \
        -d "username=${KC_BOOTSTRAP_ADMIN_USERNAME:-admin}" \
        -d "password=${KC_BOOTSTRAP_ADMIN_PASSWORD:-admin}" \
        -d "grant_type=password" \
        -d "client_id=admin-cli" 2>/dev/null)
    
    echo "$response" | grep -o '"access_token"[[:space:]]*:[[:space:]]*"[^"]*"' | sed 's/.*"access_token"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/' || echo ""
}

# Inicia o Keycloak em background
echo "Iniciando Keycloak em background..."
/opt/keycloak/bin/kc.sh start --optimized &
KEYCLOAK_PID=$!

# Aguarda o Keycloak ficar pronto
echo "Aguardando Keycloak ficar pronto..."
for i in {1..60}; do
    if curl -f http://localhost:8080/health/ready > /dev/null 2>&1; then
        echo "✓ Keycloak está pronto!"
        break
    fi
    if [ $i -eq 60 ]; then
        echo "✗ Timeout aguardando Keycloak"
        kill $KEYCLOAK_PID 2>/dev/null || true
        exit 1
    fi
    sleep 2
done

# Aguarda um pouco mais para garantir inicialização completa
sleep 5

# Processa arquivos JSON de realm
if [ -d "/opt/keycloak/data/import" ] && [ -n "$(ls -A /opt/keycloak/data/import/*.json 2>/dev/null)" ]; then
    echo "Arquivos de realm encontrados. Processando..."
    
    ADMIN_TOKEN=$(get_admin_token)
    if [ -z "$ADMIN_TOKEN" ]; then
        echo "⚠ Aviso: Não foi possível obter token de admin. Pulando importação parcial."
    else
        for realm_file in /opt/keycloak/data/import/*.json; do
            if [ ! -f "$realm_file" ]; then
                continue
            fi
            
            echo "Processando: $(basename $realm_file)"
            
            # Extrai nome do realm do JSON
            REALM_NAME=$(grep -oP '"realm"\s*:\s*"\K[^"]+' "$realm_file" | head -1)
            
            if [ -z "$REALM_NAME" ]; then
                echo "  ⚠ Não foi possível extrair nome do realm. Pulando..."
                continue
            fi
            
            echo "  Realm: $REALM_NAME"
            
            # Verifica se realm existe
            HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" \
                -H "Authorization: Bearer $ADMIN_TOKEN" \
                "http://localhost:8080/admin/realms/$REALM_NAME")
            
            if [ "$HTTP_CODE" = "200" ]; then
                echo "  Realm existe. Fazendo importação parcial (OVERWRITE)..."
                
                # Lê JSON e faz importação parcial
                REALM_JSON=$(cat "$realm_file")
                
                HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" -X POST \
                    "http://localhost:8080/admin/realms/$REALM_NAME/partialImport" \
                    -H "Authorization: Bearer $ADMIN_TOKEN" \
                    -H "Content-Type: application/json" \
                    -d "{\"ifResourceExists\":\"OVERWRITE\",\"realm\":$REALM_JSON}")
                
                if [ "$HTTP_CODE" = "200" ] || [ "$HTTP_CODE" = "204" ]; then
                    echo "  ✓ Realm atualizado com sucesso"
                else
                    echo "  ⚠ Código HTTP: $HTTP_CODE (pode ser normal se não houver mudanças)"
                fi
            else
                echo "  Realm não existe. Será criado na próxima inicialização com --import-realm"
            fi
        done
    fi
else
    echo "Nenhum arquivo de realm encontrado em /opt/keycloak/data/import"
fi

echo ""
echo "Keycloak está rodando. Pressione Ctrl+C para parar."
echo ""

# Mantém o processo rodando
wait $KEYCLOAK_PID

