#!/bin/bash
set -e

echo "Resetando senha do usuário admin para 'admin'..."

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

echo "Keycloak está pronto. Resetando senha..."

# Usa o kcadm.sh para resetar a senha
/opt/keycloak/bin/kcadm.sh config credentials \
    --server http://localhost:8080 \
    --realm master \
    --user admin \
    --password admin 2>/dev/null || true

# Se não conseguir logar, tenta criar/atualizar o usuário via API
# Primeiro, tenta obter um token admin
TOKEN=$(curl -s -X POST "http://localhost:8080/realms/master/protocol/openid-connect/token" \
    -H "Content-Type: application/x-www-form-urlencoded" \
    -d "username=admin" \
    -d "password=admin" \
    -d "grant_type=password" \
    -d "client_id=admin-cli" | jq -r '.access_token' 2>/dev/null)

if [ -z "$TOKEN" ] || [ "$TOKEN" = "null" ]; then
    echo "Não foi possível obter token. Tentando criar usuário admin via script SQL ou API..."
    echo "Execute manualmente no container:"
    echo "  sudo docker exec -it keycloak /opt/keycloak/bin/kcadm.sh create users -r master -s username=admin -s enabled=true -s email=admin@localhost"
    echo "  sudo docker exec -it keycloak /opt/keycloak/bin/kcadm.sh set-password -r master --username admin --new-password admin"
    exit 1
fi

echo "Token obtido. Resetando senha do admin..."
USER_ID=$(curl -s -X GET "http://localhost:8080/admin/realms/master/users?username=admin" \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" | jq -r '.[0].id' 2>/dev/null)

if [ -z "$USER_ID" ] || [ "$USER_ID" = "null" ]; then
    echo "Usuário admin não encontrado. Criando..."
    curl -s -X POST "http://localhost:8080/admin/realms/master/users" \
        -H "Authorization: Bearer $TOKEN" \
        -H "Content-Type: application/json" \
        -d '{"username":"admin","enabled":true,"email":"admin@localhost","credentials":[{"type":"password","value":"admin","temporary":false}]}'
else
    echo "Usuário admin encontrado. Resetando senha..."
    curl -s -X PUT "http://localhost:8080/admin/realms/master/users/$USER_ID/reset-password" \
        -H "Authorization: Bearer $TOKEN" \
        -H "Content-Type: application/json" \
        -d '{"type":"password","value":"admin","temporary":false}'
fi

echo "Senha resetada com sucesso!"
echo "Usuário: admin"
echo "Senha: admin"





