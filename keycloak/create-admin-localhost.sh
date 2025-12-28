#!/bin/bash
set -e

echo "Criando admin via acesso localhost (método de bootstrap do Keycloak)..."

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

# Tenta criar o admin usando o endpoint especial de bootstrap
# O Keycloak pode ter um endpoint especial quando acessado via localhost
echo "Tentando criar admin via endpoint de bootstrap..."

# Método 1: Tentar criar via endpoint especial de bootstrap (se existir)
BOOTSTRAP_RESPONSE=$(curl -s -X POST "http://localhost:8080/admin/realms/master/users" \
    -H "Content-Type: application/json" \
    -H "X-Forwarded-For: 127.0.0.1" \
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

echo "Resposta: $BOOTSTRAP_RESPONSE"

# Se não funcionar, tenta usar as variáveis de ambiente para forçar criação
if echo "$BOOTSTRAP_RESPONSE" | grep -q "error\|401\|403"; then
    echo "Endpoint de API não funcionou. O Keycloak deve criar o admin automaticamente com KC_BOOTSTRAP_ADMIN_USERNAME e KC_BOOTSTRAP_ADMIN_PASSWORD."
    echo "Verifique se essas variáveis estão configuradas no docker-compose."
    echo "Se não funcionar, você precisará acessar https://localhost/ no servidor para criar o admin manualmente."
    exit 1
fi

echo "Verificando se o admin foi criado..."
sleep 2

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

