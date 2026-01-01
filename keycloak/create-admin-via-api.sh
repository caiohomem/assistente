#!/bin/bash
set -e

echo "Criando usuário admin via API REST do Keycloak..."

# Aguarda o Keycloak estar pronto
echo "Aguardando Keycloak estar pronto..."
timeout=60
counter=0
while ! curl -f http://localhost:8080/health/ready > /dev/null 2>&1; do
    if [ $counter -ge $timeout ]; then
        echo "Timeout aguardando Keycloak ficar pronto"
        exit 1
    fi
    echo "Aguardando... ($counter/$timeout)"
    sleep 2
    counter=$((counter + 2))
done

echo "Keycloak está pronto."

# Tenta obter um token usando as variáveis de bootstrap (se o Keycloak suportar)
# Se não funcionar, vamos criar o usuário via API usando um método alternativo

# Método 1: Tentar criar via endpoint de bootstrap (se disponível)
echo "Tentando criar admin via endpoint de bootstrap..."
BOOTSTRAP_RESPONSE=$(curl -s -X POST "http://localhost:8080/admin/realms/master/users" \
    -H "Content-Type: application/json" \
    -d '{
        "username": "admin",
        "enabled": true,
        "email": "admin@localhost",
        "emailVerified": true,
        "credentials": [{
            "type": "password",
            "value": "admin",
            "temporary": false
        }]
    }' 2>&1)

echo "Resposta do bootstrap: $BOOTSTRAP_RESPONSE"

# Se não funcionar, tenta usar o kcadm.sh com método alternativo
if echo "$BOOTSTRAP_RESPONSE" | grep -q "error\|401\|403"; then
    echo "Bootstrap não funcionou. Tentando método alternativo..."
    
    # Método 2: Usar kcadm.sh para criar o usuário
    # Primeiro, tenta obter um token admin (pode falhar se não houver admin)
    echo "Tentando criar admin via kcadm.sh..."
    
    # Cria o usuário diretamente (sem autenticação, se o Keycloak permitir)
    /opt/keycloak/bin/kcadm.sh create users -r master \
        -s username=admin \
        -s enabled=true \
        -s email=admin@localhost \
        -s emailVerified=true \
        2>&1 || echo "kcadm.sh falhou, tentando próximo método..."
    
    # Define a senha
    /opt/keycloak/bin/kcadm.sh set-password -r master \
        --username admin \
        --new-password admin \
        2>&1 || echo "set-password falhou"
fi

echo "Verificando se o admin foi criado..."
ADMIN_CHECK=$(curl -s -X POST "http://localhost:8080/realms/master/protocol/openid-connect/token" \
    -H "Content-Type: application/x-www-form-urlencoded" \
    -d "username=admin&password=admin&grant_type=password&client_id=admin-cli" 2>&1)

if echo "$ADMIN_CHECK" | grep -q "access_token"; then
    echo "✓ Admin criado com sucesso! Login funcionando."
else
    echo "✗ Admin não foi criado ou senha incorreta."
    echo "Resposta: $ADMIN_CHECK"
    exit 1
fi



