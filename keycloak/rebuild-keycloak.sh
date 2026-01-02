#!/bin/bash
# Script para fazer rebuild do Keycloak com a configuração correta do banco

set -e

echo "Parando containers..."
sudo docker-compose -f docker-compose.production.yml down
sudo docker rm -f keycloak 2>&1 || true

echo "Fazendo rebuild do Keycloak com configuração PostgreSQL..."
sudo docker run --rm \
  -e KC_DB=postgres \
  -e KC_DB_URL=jdbc:postgresql://ep-spring-star-abbjctg7-pooler.eu-west-2.aws.neon.tech:5432/neondb?sslmode=require \
  -e KC_DB_USERNAME=neondb_owner \
  -e KC_DB_PASSWORD=npg_dcn6oJIReT0Z \
  caiohb77/keycloak-custom:1.0 \
  build

echo "Subindo containers..."
sudo docker-compose -f docker-compose.production.yml up -d

echo "Aguardando inicialização..."
sleep 10

echo "Verificando logs..."
sudo docker logs keycloak --tail=50





