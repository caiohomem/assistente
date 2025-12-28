# Configurar Cloudflare Tunnel para Keycloak

O erro 521 significa que o Cloudflare Tunnel não está conectado ao Keycloak.

## Opção 1: Cloudflare Tunnel no Lightsail (Recomendado)

### 1. Instalar cloudflared no Lightsail

```bash
# No Lightsail via SSH
curl -L https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-linux-amd64 -o cloudflared
chmod +x cloudflared
sudo mv cloudflared /usr/local/bin/

# Verificar instalação
cloudflared --version
```

### 2. Autenticar no Cloudflare

```bash
cloudflared tunnel login
```

Isso abrirá o navegador para autenticar. Depois, copie o token.

### 3. Criar o tunnel

```bash
cloudflared tunnel create keycloak-tunnel
```

### 4. Configurar o tunnel

Crie o arquivo de configuração:

```bash
mkdir -p ~/.cloudflared
nano ~/.cloudflared/config.yml
```

Conteúdo:

```yaml
tunnel: keycloak-tunnel
credentials-file: /home/ubuntu/.cloudflared/[TUNNEL-ID].json

ingress:
  - hostname: keycloak.callback-local-cchagas.xyz
    service: http://localhost:8080
  - service: http_status:404
```

Substitua `[TUNNEL-ID]` pelo ID do tunnel criado.

### 5. Rodar o tunnel

```bash
# Teste primeiro
cloudflared tunnel --config ~/.cloudflared/config.yml run

# Se funcionar, configure como serviço systemd
sudo cloudflared service install
sudo systemctl start cloudflared
sudo systemctl enable cloudflared
```

### 6. Verificar status

```bash
sudo systemctl status cloudflared
```

---

## Opção 2: Cloudflare Tunnel via Dashboard (Zero Trust)

Se você está usando Cloudflare Zero Trust:

1. Acesse: https://one.dash.cloudflare.com/
2. Vá em **Networks → Tunnels**
3. Verifique se o tunnel está **Active**
4. Verifique a configuração do **Public Hostname**:
   - Hostname: `keycloak.callback-local-cchagas.xyz`
   - Service: `http://localhost:8080` (ou `http://99.80.217.123:8080`)

---

## Opção 3: Verificar se o Tunnel está em outro servidor

Se o Cloudflare Tunnel está rodando em outro servidor/máquina:

1. Verifique se o tunnel está apontando para: `http://99.80.217.123:8080`
2. Ou configure para apontar para: `http://localhost:8080` (se estiver no mesmo servidor)

---

## Troubleshooting

### Erro 521: Web Server Is Down

- Verifique se o Keycloak está rodando: `sudo docker ps | grep keycloak`
- Verifique se responde localmente: `curl http://localhost:8080`
- Verifique se o Cloudflare Tunnel está rodando: `ps aux | grep cloudflared`
- Verifique os logs do tunnel: `sudo journalctl -u cloudflared -f`

### Verificar conectividade

```bash
# No Lightsail
curl http://localhost:8080

# Deve retornar um redirect 302 para /admin/
```

---

## Configuração Rápida (Script)

Crie um script `setup-tunnel.sh`:

```bash
#!/bin/bash
set -e

echo "Instalando cloudflared..."
curl -L https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-linux-amd64 -o /tmp/cloudflared
chmod +x /tmp/cloudflared
sudo mv /tmp/cloudflared /usr/local/bin/

echo "Instalação concluída!"
echo "Execute: cloudflared tunnel login"
echo "Depois: cloudflared tunnel create keycloak-tunnel"
```

