# Quick Start - Servidor MCP

Guia rápido para começar a usar o servidor MCP em 5 minutos.

## ⚡ Passos Rápidos

### 1. Instalar e Compilar
```bash
cd mcp-server
npm install
npm run build
```

### 2. Obter Token
```powershell
# Windows
.\scripts\get-token.ps1 -Email "seu_email@exemplo.com" -Password "sua_senha" -Save

# Linux/macOS
node scripts/get-token.js seu_email@exemplo.com sua_senha --save
```

### 3. Configurar no Cursor

Edite o arquivo de configuração do Cursor:

**Windows:**
```
%APPDATA%\Cursor\User\globalStorage\saoudrizwan.claude-dev\settings\cline_mcp_settings.json
```

**macOS:**
```
~/Library/Application Support/Cursor/User/globalStorage/saoudrizwan.claude-dev/settings/cline_mcp_settings.json
```

**Linux:**
```
~/.config/Cursor/User/globalStorage/saoudrizwan.claude-dev/settings/cline_mcp_settings.json
```

Adicione:

```json
{
  "mcpServers": {
    "assistente-executivo": {
      "command": "node",
      "args": [
        "CAMINHO_ABSOLUTO_PARA/mcp-server/dist/index.js"
      ],
      "env": {
        "API_BASE_URL": "http://localhost:5239",
        "ACCESS_TOKEN": "COLE_O_TOKEN_AQUI"
      }
    }
  }
}
```

**⚠️ IMPORTANTE:**
- Substitua `CAMINHO_ABSOLUTO_PARA` pelo caminho completo do seu projeto
- Substitua `COLE_O_TOKEN_AQUI` pelo token obtido no passo 2

### 4. Reiniciar Cursor

Feche e abra o Cursor novamente.

### 5. Testar

No Cursor, digite:
```
Liste meus contatos
```

Pronto! 🎉

## 🔄 Renovar Token (quando expirar)

Tokens expiram em 1 hora. Para renovar:

```powershell
# Windows
.\scripts\get-token.ps1 -Email "seu_email@exemplo.com" -Password "sua_senha" -Save

# Linux/macOS
node scripts/get-token.js seu_email@exemplo.com sua_senha --save
```

Depois atualize o `ACCESS_TOKEN` na configuração do Cursor.

## ❓ Problemas?

Veja o [GUIA_USO.md](./GUIA_USO.md) para troubleshooting detalhado.





