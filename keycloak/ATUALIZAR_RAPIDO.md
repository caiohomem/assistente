# ⚡ Atualização Rápida - Keycloak no Lightsail

Você já tem o Keycloak configurado. Só precisa atualizar o docker-compose.yml e baixar a nova imagem.

## 🚀 Opção 1: Script Automatizado (Windows)

No PowerShell (Windows):

```powershell
cd keycloak
.\send-compose-to-lightsail.ps1 [IP_DO_LIGHTSAIL] ubuntu 1.0
```

Isso vai enviar o docker-compose.yml atualizado para o Lightsail.

## 📋 Opção 2: Manual

### 1. Envie o docker-compose.yml atualizado

No PowerShell (Windows):

```powershell
cd keycloak
scp docker-compose.production.yml ubuntu@SEU_IP:~/keycloak/
```

### 2. No Lightsail, execute:

```bash
cd ~/keycloak

# Para os containers
docker compose -f docker-compose.production.yml down

# Baixa a nova imagem do Docker Hub
docker pull caiohb77/keycloak-custom:1.0

# Sobe novamente
docker compose -f docker-compose.production.yml up -d

# Verifica os logs
docker logs -f keycloak --tail=200
```

## ✅ Pronto!

O Keycloak será atualizado com a nova imagem que tem o tema customizado embutido.

---

## 🔍 Verificar se funcionou

```bash
# Verifica se está rodando
docker ps | grep keycloak

# Verifica a versão da imagem
docker images | grep keycloak-custom

# Testa o health check
curl http://localhost:8080/health/ready
```

## 🎨 Verificar o tema

1. Acesse: `http://SEU_IP:8080`
2. Login: `admin` / `admin`
3. Vá em: **Realm Settings → Themes**
4. Verifique se **Login theme** está como `assistenteexecutivo`

