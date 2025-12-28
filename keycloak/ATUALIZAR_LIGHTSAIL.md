# 🚀 Atualizar Keycloak no Lightsail

Agora que a imagem está no Docker Hub, siga estes passos para atualizar no Lightsail:

## ⚡ Atualização Rápida (Recomendado)

### 1. Conecte-se ao Lightsail

```bash
ssh ubuntu@SEU_IP_LIGHTSAIL
```

### 2. Execute os comandos

```bash
cd ~/keycloak

# Para os containers
docker compose -f docker-compose.production.yml down

# Baixa a nova imagem
docker pull caiohb77/keycloak-custom:1.0

# Sobe novamente
docker compose -f docker-compose.production.yml up -d

# Verifica os logs
docker logs -f keycloak --tail=200
```

**Pronto!** O Keycloak será atualizado com a nova versão.

---

## 📋 Passo a Passo Detalhado

### 1. Conecte-se ao Lightsail

```bash
ssh ubuntu@SEU_IP_LIGHTSAIL
```

### 2. Vá para o diretório do Keycloak

```bash
cd ~/keycloak
```

Se a pasta não existir, crie:

```bash
mkdir -p ~/keycloak
cd ~/keycloak
```

### 3. Faça login no Docker Hub (se necessário)

```bash
docker login -u caiohb77
```

Quando solicitado, use seu **PAT** (Personal Access Token).

### 4. Pare os containers

```bash
docker compose -f docker-compose.production.yml down
```

### 5. Baixe a nova imagem

```bash
docker pull caiohb77/keycloak-custom:1.0
```

### 6. Verifique o docker-compose.yml

Certifique-se de que está usando a imagem correta:

```bash
cat docker-compose.production.yml | grep image:
```

Deve mostrar:
```yaml
image: caiohb77/keycloak-custom:1.0
```

Se não estiver correto, edite:

```bash
nano docker-compose.production.yml
```

Ou use sed:

```bash
sed -i 's|image:.*keycloak-custom.*|image: caiohb77/keycloak-custom:1.0|g' docker-compose.production.yml
```

### 7. Suba os containers

```bash
docker compose -f docker-compose.production.yml up -d
```

### 8. Verifique os logs

```bash
docker logs -f keycloak --tail=200
```

Pressione `Ctrl+C` para sair dos logs.

### 9. Verifique se está funcionando

```bash
curl http://localhost:8080/health/ready
```

Deve retornar algo como `{"status":"UP"}`.

---

## 🔄 Para Próximas Atualizações

Quando você publicar uma nova versão (ex: 1.1), apenas execute:

```bash
cd ~/keycloak
docker compose down
docker pull caiohb77/keycloak-custom:1.1
# Atualize a versão no docker-compose.yml se necessário
docker compose up -d
docker logs -f keycloak
```

---

## 🛠️ Troubleshooting

### Erro: "unauthorized"

```bash
docker login -u caiohb77
# Use o PAT
```

### Erro: "manifest unknown"

Verifique se a versão existe:
- https://hub.docker.com/r/caiohb77/keycloak-custom/tags

### Container não inicia

```bash
# Veja os logs detalhados
docker logs keycloak

# Verifique o status
docker ps -a

# Verifique o PostgreSQL
docker logs kc-postgres
```

### Limpar espaço (remover imagens antigas)

```bash
# Remove imagens não utilizadas
docker image prune -a

# Remove uma versão específica antiga
docker rmi caiohb77/keycloak-custom:0.9
```

---

## ✅ Verificação Final

1. **Acesse o Keycloak**: `http://SEU_IP:8080`
2. **Login**: `admin` / `admin`
3. **Verifique o tema**: Realm Settings → Themes → Login theme: `assistenteexecutivo`

---

## 📝 Notas

- O PostgreSQL **não** será afetado (dados preservados)
- A atualização é **rápida** (apenas baixa a nova imagem)
- Se algo der errado, você pode voltar para a versão anterior facilmente

