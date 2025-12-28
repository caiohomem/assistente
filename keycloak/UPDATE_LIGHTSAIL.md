# Atualizar Keycloak no Lightsail

Guia rápido para atualizar o Keycloak no Lightsail quando uma nova versão é publicada no Docker Hub.

## Método 1: Script Automatizado (Recomendado)

### 1. Conecte-se ao Lightsail

```bash
ssh ubuntu@SEU_IP_LIGHTSAIL
```

### 2. Vá para o diretório do Keycloak

```bash
cd ~/keycloak
```

### 3. Baixe o script de atualização (se ainda não tiver)

```bash
# Ou crie o arquivo update-lightsail.sh manualmente
chmod +x update-lightsail.sh
```

### 4. Execute o script

```bash
./update-lightsail.sh 1.0 caiohb77
```

O script irá:
- Verificar Docker
- Fazer login no Docker Hub (se necessário)
- Parar os containers
- Baixar a nova imagem
- Atualizar docker-compose.yml
- Subir os containers novamente

---

## Método 2: Comandos Manuais

### 1. Conecte-se ao Lightsail

```bash
ssh ubuntu@SEU_IP_LIGHTSAIL
```

### 2. Vá para o diretório do Keycloak

```bash
cd ~/keycloak
```

### 3. Faça login no Docker Hub (se necessário)

```bash
docker login -u caiohb77
# Use seu PAT quando solicitado
```

### 4. Pare os containers

```bash
docker compose -f docker-compose.production.yml down
```

### 5. Baixe a nova imagem

```bash
docker pull caiohb77/keycloak-custom:1.0
```

### 6. Verifique/Atualize o docker-compose.yml

Certifique-se de que está usando a versão correta:

```yaml
keycloak:
  image: caiohb77/keycloak-custom:1.0  # Verifique a versão aqui
```

Se precisar atualizar:

```bash
nano docker-compose.production.yml
# Ou use sed:
sed -i 's|image:.*keycloak-custom.*|image: caiohb77/keycloak-custom:1.0|g' docker-compose.production.yml
```

### 7. Suba os containers novamente

```bash
docker compose -f docker-compose.production.yml up -d
```

### 8. Verifique os logs

```bash
docker logs -f keycloak --tail=200
```

### 9. Verifique se está funcionando

```bash
curl http://localhost:8080/health/ready
```

---

## Atualização Rápida (One-liner)

Se você já está logado e o docker-compose.yml está correto:

```bash
cd ~/keycloak && \
docker compose -f docker-compose.production.yml down && \
docker pull caiohb77/keycloak-custom:1.0 && \
docker compose -f docker-compose.production.yml up -d && \
docker logs -f keycloak --tail=50
```

---

## Verificar Versão Atual

Para ver qual versão está rodando:

```bash
docker inspect keycloak | grep -i image
```

Ou:

```bash
docker images | grep keycloak-custom
```

---

## Troubleshooting

### Erro: "unauthorized: authentication required"

```bash
docker login -u caiohb77
# Use o PAT
```

### Erro: "manifest unknown"

- Verifique se a versão existe no Docker Hub: https://hub.docker.com/r/caiohb77/keycloak-custom/tags
- Verifique se o username está correto

### Container não inicia

```bash
# Veja os logs
docker logs keycloak --tail=200

# Verifique o status
docker compose -f docker-compose.production.yml ps

# Verifique se o PostgreSQL está rodando
docker logs kc-postgres --tail=50
```

### Limpar imagens antigas (liberar espaço)

```bash
# Remove imagens não utilizadas
docker image prune -a

# Remove imagens específicas antigas
docker rmi caiohb77/keycloak-custom:0.9  # versão antiga
```

---

## Dicas

1. **Sempre faça backup** antes de atualizar (se tiver dados importantes):
   ```bash
   docker exec kc-postgres pg_dump -U keycloak keycloak > backup_$(date +%Y%m%d).sql
   ```

2. **Teste em horário de baixo tráfego** se possível

3. **Monitore os logs** após a atualização:
   ```bash
   docker logs -f keycloak
   ```

4. **Verifique o tema** após atualizar:
   - Acesse o Admin Console
   - Vá em Realm Settings → Themes
   - Verifique se o tema `assistenteexecutivo` ainda está selecionado

