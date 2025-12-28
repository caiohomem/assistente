#!/bin/bash
# Script de atualização do Keycloak no Lightsail
# Uso: ./update-lightsail.sh [version] [dockerhub-username]
# Exemplo: ./update-lightsail.sh 1.0 caiohb77

set -e

VERSION=${1:-"1.0"}
DOCKERHUB_USER=${2:-"caiohb77"}
IMAGE_NAME="keycloak-custom"
DOCKERHUB_TAG="${DOCKERHUB_USER}/${IMAGE_NAME}:${VERSION}"
COMPOSE_FILE="docker-compose.production.yml"

echo "========================================="
echo "Atualizando Keycloak no Lightsail"
echo "========================================="
echo ""
echo "Versão: $VERSION"
echo "Imagem: $DOCKERHUB_TAG"
echo ""

# Verifica se está no diretório correto
if [ ! -f "$COMPOSE_FILE" ]; then
    echo "✗ Arquivo $COMPOSE_FILE não encontrado!"
    echo "  Certifique-se de estar no diretório ~/keycloak"
    exit 1
fi

# Verifica se Docker está rodando
echo "Verificando Docker..."
if ! docker info > /dev/null 2>&1; then
    echo "✗ Docker não está rodando"
    exit 1
fi
echo "✓ Docker está rodando"
echo ""

# Login no Docker Hub (se necessário)
echo "Verificando login no Docker Hub..."
if ! docker info 2>&1 | grep -q "Username"; then
    echo "⚠ Não está logado no Docker Hub"
    echo "Execute: docker login -u $DOCKERHUB_USER"
    echo "Use seu PAT (Personal Access Token)"
    read -p "Deseja fazer login agora? (S/N) " -n 1 -r
    echo ""
    if [[ $REPLY =~ ^[Ss]$ ]]; then
        docker login -u $DOCKERHUB_USER
        if [ $? -ne 0 ]; then
            echo "✗ Erro ao fazer login"
            exit 1
        fi
    else
        echo "✗ Login necessário para continuar"
        exit 1
    fi
else
    echo "✓ Já está logado no Docker Hub"
fi
echo ""

# Para os containers
echo "Parando containers..."
docker compose -f $COMPOSE_FILE down
echo "✓ Containers parados"
echo ""

# Remove a imagem antiga (opcional, libera espaço)
read -p "Deseja remover a imagem antiga? (S/N) " -n 1 -r
echo ""
if [[ $REPLY =~ ^[Ss]$ ]]; then
    echo "Removendo imagem antiga..."
    docker rmi $DOCKERHUB_TAG 2>/dev/null || true
    echo "✓ Imagem antiga removida"
    echo ""
fi

# Faz pull da nova imagem
echo "Baixando nova imagem do Docker Hub..."
docker pull $DOCKERHUB_TAG
if [ $? -ne 0 ]; then
    echo "✗ Erro ao baixar a imagem"
    exit 1
fi
echo "✓ Imagem baixada com sucesso"
echo ""

# Atualiza o docker-compose.yml se necessário
echo "Verificando docker-compose.yml..."
CURRENT_IMAGE=$(grep "image:" $COMPOSE_FILE | grep keycloak | awk '{print $2}' | tr -d '"')
if [ "$CURRENT_IMAGE" != "$DOCKERHUB_TAG" ]; then
    echo "⚠ Imagem no docker-compose.yml é diferente: $CURRENT_IMAGE"
    echo "Atualizando para: $DOCKERHUB_TAG"
    sed -i "s|image:.*keycloak-custom.*|image: $DOCKERHUB_TAG|g" $COMPOSE_FILE
    echo "✓ docker-compose.yml atualizado"
else
    echo "✓ docker-compose.yml já está correto"
fi
echo ""

# Sobe os containers
echo "Subindo containers com a nova imagem..."
docker compose -f $COMPOSE_FILE up -d
if [ $? -ne 0 ]; then
    echo "✗ Erro ao subir os containers"
    exit 1
fi
echo "✓ Containers iniciados"
echo ""

# Aguarda um pouco e verifica os logs
echo "Aguardando inicialização (10 segundos)..."
sleep 10

echo ""
echo "========================================="
echo "Atualização concluída!"
echo "========================================="
echo ""
echo "Verificando status dos containers:"
docker compose -f $COMPOSE_FILE ps
echo ""
echo "Para ver os logs:"
echo "  docker logs -f keycloak --tail=200"
echo ""
echo "Para verificar se está funcionando:"
echo "  curl http://localhost:8080/health/ready"
echo ""

