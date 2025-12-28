# Guia de Uso do Servidor MCP

Este guia explica passo a passo como instalar, configurar e usar o servidor MCP do Assistente Executivo.

## 📋 Pré-requisitos

- Node.js 18+ instalado
- Acesso à API do Assistente Executivo
- Credenciais de usuário no Keycloak (email e senha)
- Cursor IDE (ou outra ferramenta compatível com MCP)

## 🚀 Passo 1: Instalação

### 1.1 Instalar Dependências

```bash
cd mcp-server
npm install
```

### 1.2 Compilar o Projeto

```bash
npm run build
```

Isso criará a pasta `dist/` com os arquivos compilados.

## 🔑 Passo 2: Obter Token de Acesso

### 2.1 Usar Script (Recomendado)

**Windows (PowerShell):**
```powershell
.\scripts\get-token.ps1 -Email "seu_email@exemplo.com" -Password "sua_senha" -Save
```

**Linux/macOS:**
```bash
node scripts/get-token.js seu_email@exemplo.com sua_senha --save
```

O script irá:
- ✅ Obter o token do Keycloak
- ✅ Mostrar o token na tela
- ✅ Salvar no arquivo `.env.local` (se usar `-Save`)

### 2.2 Copiar o Token

Anote o `access_token` que aparece na tela. Você precisará dele na próxima etapa.

**Exemplo de token:**
```
eyJhbGciOiJSUzI1NiIsInR5cCIgOiAiSldUIiwia2lkIiA6ICJ...
```

## ⚙️ Passo 3: Configurar no Cursor

### 3.1 Localizar Arquivo de Configuração

O Cursor armazena a configuração do MCP em um arquivo JSON. O caminho varia por sistema:

**Windows:**
```
%APPDATA%\Cursor\User\globalStorage\saoudrizwan.claude-dev\settings\cline_mcp_settings.json
```

Ou:
```
C:\Users\SEU_USUARIO\AppData\Roaming\Cursor\User\globalStorage\saoudrizwan.claude-dev\settings\cline_mcp_settings.json
```

**macOS:**
```
~/Library/Application Support/Cursor/User/globalStorage/saoudrizwan.claude-dev/settings/cline_mcp_settings.json
```

**Linux:**
```
~/.config/Cursor/User/globalStorage/saoudrizwan.claude-dev/settings/cline_mcp_settings.json
```

### 3.2 Editar Configuração

Abra o arquivo `cline_mcp_settings.json` e adicione a configuração do servidor MCP:

```json
{
  "mcpServers": {
    "assistente-executivo": {
      "command": "node",
      "args": [
        "C:\\Projects\\AssistenteExecutivo\\mcp-server\\dist\\index.js"
      ],
      "env": {
        "API_BASE_URL": "http://localhost:5239",
        "ACCESS_TOKEN": "seu_token_jwt_aqui"
      }
    }
  }
}
```

**⚠️ IMPORTANTE:**
- Substitua `C:\\Projects\\AssistenteExecutivo\\mcp-server\\dist\\index.js` pelo caminho **absoluto** do seu projeto
- Substitua `seu_token_jwt_aqui` pelo token obtido no Passo 2
- Ajuste `API_BASE_URL` se sua API estiver em outro endereço

**Exemplo para macOS/Linux:**
```json
{
  "mcpServers": {
    "assistente-executivo": {
      "command": "node",
      "args": [
        "/Users/seu_usuario/Projects/AssistenteExecutivo/mcp-server/dist/index.js"
      ],
      "env": {
        "API_BASE_URL": "http://localhost:5239",
        "ACCESS_TOKEN": "seu_token_jwt_aqui"
      }
    }
  }
}
```

### 3.3 Reiniciar Cursor

Após salvar o arquivo de configuração, **reinicie o Cursor** para que as mudanças tenham efeito.

## ✅ Passo 4: Verificar Instalação

### 4.1 Verificar se o MCP está Funcionando

1. Abra o Cursor
2. Abra o chat/assistente
3. Tente usar uma ferramenta do MCP, por exemplo:

```
Liste meus contatos
```

Ou:

```
Crie um contato chamado João Silva da empresa XYZ
```

### 4.2 Verificar Logs

Se algo não funcionar, verifique:

1. **Console do Cursor**: Abra o Developer Tools (Ctrl+Shift+I ou Cmd+Option+I)
2. **Logs do servidor**: O servidor MCP imprime logs no stderr, que podem aparecer no console

## 🎯 Passo 5: Usar as Ferramentas

Agora você pode usar todas as ferramentas do MCP através do Cursor! Aqui estão alguns exemplos:

### Exemplos de Uso

#### Criar um Contato
```
Crie um novo contato:
- Nome: Maria Santos
- Empresa: Tech Corp
- Cargo: Desenvolvedora
- Email: maria@techcorp.com
```

#### Listar Contatos
```
Mostre meus contatos
```

#### Buscar Contatos
```
Busque contatos da empresa "Tech Corp"
```

#### Criar um Lembrete
```
Crie um lembrete para entrar em contato com o João Silva amanhã às 10h
```

#### Listar Lembretes
```
Mostre meus lembretes pendentes
```

#### Criar uma Nota
```
Adicione uma nota ao contato João Silva: "Interessado em nosso produto X"
```

#### Verificar Créditos
```
Quantos créditos eu tenho disponíveis?
```

## 🔧 Troubleshooting

### Problema: "Ferramenta não encontrada"

**Solução:**
1. Verifique se compilou o projeto: `npm run build`
2. Verifique se o caminho no arquivo de configuração está correto (deve ser absoluto)
3. Reinicie o Cursor

### Problema: "Erro de autenticação" ou "401 Unauthorized"

**Solução:**
1. Verifique se o token não expirou (tokens expiram em 1 hora)
2. Obtenha um novo token usando o script
3. Atualize o `ACCESS_TOKEN` na configuração
4. Reinicie o Cursor

### Problema: "Erro de conexão" ou "API não encontrada"

**Solução:**
1. Verifique se a API está rodando
2. Verifique se `API_BASE_URL` está correto
3. Teste a API diretamente: `curl http://localhost:5239/health`

### Problema: Token expirado

**Solução:**
1. Use o script novamente para obter um novo token
2. Atualize o `ACCESS_TOKEN` na configuração
3. Reinicie o Cursor

Ou use o refresh token:
```bash
curl -X POST "https://keycloak.callback-local-cchagas.xyz/realms/assistenteexecutivo/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=refresh_token" \
  -d "client_id=assistenteexecutivo-app" \
  -d "refresh_token=seu_refresh_token_aqui"
```

## 📝 Exemplo Completo de Configuração

Aqui está um exemplo completo do arquivo `cline_mcp_settings.json`:

```json
{
  "mcpServers": {
    "assistente-executivo": {
      "command": "node",
      "args": [
        "C:\\Projects\\AssistenteExecutivo\\mcp-server\\dist\\index.js"
      ],
      "env": {
        "API_BASE_URL": "http://localhost:5239",
        "ACCESS_TOKEN": "eyJhbGciOiJSUzI1NiIsInR5cCIgOiAiSldUIiwia2lkIiA6ICJ..."
      }
    }
  }
}
```

## 🔄 Atualizar Token Automaticamente

Para não precisar atualizar o token manualmente toda vez, você pode:

1. **Usar um script de renovação automática** (criar um script que renova o token periodicamente)
2. **Usar refresh token** (implementar lógica para renovar automaticamente)
3. **Criar um client com Service Account** (mais avançado, permite tokens de longa duração)

## 📚 Ferramentas Disponíveis

O servidor MCP fornece acesso a todas estas ferramentas:

### Contatos
- `list_contacts` - Listar contatos
- `search_contacts` - Buscar contatos
- `get_contact` - Obter contato por ID
- `create_contact` - Criar contato
- `update_contact` - Atualizar contato
- `delete_contact` - Deletar contato
- `add_contact_email` - Adicionar email
- `add_contact_phone` - Adicionar telefone
- `add_contact_tag` - Adicionar tag
- `add_contact_relationship` - Adicionar relacionamento

### Lembretes
- `create_reminder` - Criar lembrete
- `list_reminders` - Listar lembretes
- `get_reminder` - Obter lembrete
- `update_reminder_status` - Atualizar status
- `delete_reminder` - Deletar lembrete

### Notas
- `list_contact_notes` - Listar notas
- `get_note` - Obter nota
- `create_text_note` - Criar nota
- `update_note` - Atualizar nota
- `delete_note` - Deletar nota

### Automação
- Drafts, Templates, Letterheads (CRUD completo)

### Créditos
- `get_credit_balance` - Ver saldo
- `list_credit_transactions` - Listar transações
- `list_credit_packages` - Listar pacotes
- `purchase_credit_package` - Comprar pacote

E muito mais! Veja o README.md para a lista completa.

## 🎉 Pronto!

Agora você está pronto para usar o servidor MCP! Experimente fazer algumas perguntas ao Cursor usando as ferramentas disponíveis.





