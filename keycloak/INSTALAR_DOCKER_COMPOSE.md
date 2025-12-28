# Instalar Docker Compose no Lightsail

## Método 1: Automático (via script)

O script `build.ps1` já instala automaticamente. Basta executar:

```powershell
.\build.ps1 1.0 caiohb77 99.80.217.123
```

## Método 2: Manual via SSH

### Opção A: Docker Compose Standalone (Recomendado)

```bash
# Conecte-se ao Lightsail
ssh -i ~/Downloads/lightsail-key.pem ubuntu@99.80.217.123

# Instala docker-compose
sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose

# Dá permissão de execução
sudo chmod +x /usr/local/bin/docker-compose

# Verifica instalação
docker-compose --version
```

### Opção B: Docker Compose Plugin (Mais moderno)

```bash
# Conecte-se ao Lightsail
ssh -i ~/Downloads/lightsail-key.pem ubuntu@99.80.217.123

# Atualiza pacotes
sudo apt-get update

# Instala o plugin
sudo apt-get install docker-compose-plugin

# Verifica instalação
docker compose version
```

## Método 3: Via Script PowerShell (Windows)

Execute no PowerShell:

```powershell
$keyPath = "$env:USERPROFILE\Downloads\lightsail-key.pem"
$LIGHTSAIL_IP = "99.80.217.123"

# Instala docker-compose
ssh -i $keyPath ubuntu@$LIGHTSAIL_IP "sudo curl -L 'https://github.com/docker/compose/releases/latest/download/docker-compose-linux-x86_64' -o /usr/local/bin/docker-compose && sudo chmod +x /usr/local/bin/docker-compose && docker-compose --version"
```

## Verificar se está instalado

```bash
# Verifica versão standalone
docker-compose --version

# Verifica versão plugin
docker compose version

# Verifica qual está disponível
which docker-compose
docker compose version
```

## Troubleshooting

### Erro: "Permission denied"

```bash
# Adiciona usuário ao grupo docker
sudo usermod -aG docker $USER
# Desconecte e reconecte para aplicar
```

### Erro: "command not found"

```bash
# Verifica se está no PATH
echo $PATH
# Adiciona ao PATH se necessário
export PATH=$PATH:/usr/local/bin
```

### Erro: "Cannot connect to Docker daemon"

```bash
# Inicia o serviço Docker
sudo systemctl start docker
sudo systemctl enable docker
```

