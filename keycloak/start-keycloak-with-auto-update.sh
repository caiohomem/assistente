#!/bin/bash
set -e

echo "Building Keycloak with PostgreSQL configuration..."
/opt/keycloak/bin/kc.sh build --db=postgres

# Inicia o Keycloak em background para poder fazer importação parcial depois
echo "Iniciando Keycloak..."
/opt/keycloak/bin/kc.sh start --optimized &
KEYCLOAK_PID=$!

# Função para limpar processo em caso de erro
cleanup() {
    if [ ! -z "$KEYCLOAK_PID" ]; then
        kill $KEYCLOAK_PID 2>/dev/null || true
    fi
}
trap cleanup EXIT

# Aguarda o Keycloak ficar pronto
echo "Aguardando Keycloak ficar pronto..."
MAX_WAIT=120
WAIT_COUNT=0
while ! curl -f http://localhost:8080/health/ready > /dev/null 2>&1; do
    if [ $WAIT_COUNT -ge $MAX_WAIT ]; then
        echo "Timeout aguardando Keycloak ficar pronto após ${MAX_WAIT}s"
        exit 1
    fi
    echo "Aguardando Keycloak... ($WAIT_COUNT/$MAX_WAIT)"
    sleep 2
    WAIT_COUNT=$((WAIT_COUNT + 2))
done

echo "Keycloak está pronto!"

# Verifica se há arquivos de realm para importar/atualizar
if [ -d "/opt/keycloak/data/import" ] && [ "$(ls -A /opt/keycloak/data/import/*.json 2>/dev/null)" ]; then
    echo "Arquivos de realm encontrados para importação/atualização..."
    
    # Aguarda um pouco mais para garantir que o Keycloak está totalmente inicializado
    sleep 5
    
    # Tenta fazer importação parcial para cada arquivo JSON
    for realm_file in /opt/keycloak/data/import/*.json; do
        if [ -f "$realm_file" ]; then
            echo "Processando arquivo: $realm_file"
            
            # Extrai o nome do realm do arquivo JSON (assumindo que o arquivo contém "realm": "nome")
            REALM_NAME=$(grep -o '"realm"[[:space:]]*:[[:space:]]*"[^"]*"' "$realm_file" | head -1 | sed 's/.*"realm"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/')
            
            if [ -z "$REALM_NAME" ]; then
                echo "Aviso: Não foi possível extrair o nome do realm do arquivo $realm_file. Pulando..."
                continue
            fi
            
            echo "Realm identificado: $REALM_NAME"
            
            # Verifica se o realm já existe
            REALM_EXISTS=$(curl -s -o /dev/null -w "%{http_code}" \
                -H "Authorization: Bearer $(get_admin_token)" \
                "http://localhost:8080/admin/realms/$REALM_NAME" 2>/dev/null || echo "000")
            
            if [ "$REALM_EXISTS" = "200" ]; then
                echo "Realm $REALM_NAME já existe. Fazendo importação parcial (OVERWRITE)..."
                
                # Faz importação parcial via API REST
                ADMIN_TOKEN=$(get_admin_token)
                if [ -z "$ADMIN_TOKEN" ]; then
                    echo "Erro: Não foi possível obter token de admin. Pulando importação parcial."
                    continue
                fi
                
                # Lê o conteúdo do JSON
                REALM_JSON=$(cat "$realm_file")
                
                # Faz a importação parcial
                RESPONSE=$(curl -s -w "\n%{http_code}" -X POST \
                    "http://localhost:8080/admin/realms/$REALM_NAME/partialImport" \
                    -H "Authorization: Bearer $ADMIN_TOKEN" \
                    -H "Content-Type: application/json" \
                    -d "{\"ifResourceExists\":\"OVERWRITE\",\"realm\":$REALM_JSON}" 2>/dev/null)
                
                HTTP_CODE=$(echo "$RESPONSE" | tail -1)
                BODY=$(echo "$RESPONSE" | sed '$d')
                
                if [ "$HTTP_CODE" = "200" ] || [ "$HTTP_CODE" = "204" ]; then
                    echo "✓ Realm $REALM_NAME atualizado com sucesso via importação parcial"
                else
                    echo "⚠ Aviso: Importação parcial retornou código $HTTP_CODE. Resposta: $BODY"
                    echo "  Isso pode ser normal se o realm não tiver mudanças significativas."
                fi
            else
                echo "Realm $REALM_NAME não existe. Será criado na próxima inicialização com --import-realm"
                echo "  (ou você pode usar importação parcial manualmente)"
            fi
        fi
    done
else
    echo "Nenhum arquivo de realm encontrado em /opt/keycloak/data/import"
fi

# Remove o trap e mantém o Keycloak rodando
trap - EXIT

echo "Keycloak rodando. Pressione Ctrl+C para parar."
wait $KEYCLOAK_PID

