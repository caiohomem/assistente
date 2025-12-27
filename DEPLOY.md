# Guia de Deploy - Build Incremental

Este projeto utiliza um sistema de **build incremental** que detecta automaticamente quais serviços precisam ser reconstruídos e implantados com base nas mudanças no código.

## Como Funciona

O pipeline do Google Cloud Build (`cloudbuild.yaml`) detecta automaticamente mudanças usando `git diff`:

- **Mudanças em `web/`** → Build e deploy apenas da aplicação web
- **Mudanças em `backend/`** → Build e deploy apenas da API
- **Mudanças em `cloudbuild.yaml`** → Build e deploy de tudo (por segurança)
- **Primeiro commit ou sem histórico** → Build e deploy de tudo

## Comportamento Padrão

Por padrão, o sistema detecta automaticamente as mudanças. Você não precisa fazer nada especial - apenas faça commit e push normalmente.

### Exemplos

```bash
# Alterar apenas arquivos da web
git add web/src/components/MyComponent.tsx
git commit -m "Atualiza componente"
git push
# → Apenas a web será construída e implantada

# Alterar apenas arquivos da API
git add backend/src/AssistenteExecutivo.Api/Controllers/MyController.cs
git commit -m "Adiciona novo endpoint"
git push
# → Apenas a API será construída e implantada

# Alterar ambos
git add web/src/... backend/src/...
git commit -m "Atualiza web e API"
git push
# → Ambos serão construídos e implantados
```

## Forçar Build Completo

Se você precisar forçar o build de todos os serviços (mesmo sem mudanças), defina a substituição `_FORCE_BUILD_ALL` no trigger do Cloud Build:

```bash
# Via gcloud CLI
gcloud builds submit --config=cloudbuild.yaml \
  --substitutions=_FORCE_BUILD_ALL=true,_NEXT_PUBLIC_API_BASE_URL=https://...
```

Ou configure no Console do Google Cloud:
1. Vá para **Cloud Build > Triggers**
2. Edite o trigger
3. Adicione a substituição: `_FORCE_BUILD_ALL` = `true`

## Configuração do Trigger

### Substituições Necessárias

Configure estas substituições no trigger do Cloud Build:

| Substituição | Descrição | Exemplo |
|-------------|-----------|---------|
| `_NEXT_PUBLIC_API_BASE_URL` | URL pública da API em produção | `https://assistente-api-xxxxx-uc.a.run.app` |
| `_CLOUD_RUN_SERVICE_WEB` | Nome do serviço Cloud Run da web | `assistente-web` |
| `_CLOUD_RUN_SERVICE_API` | Nome do serviço Cloud Run da API | `assistente-api` |
| `_CLOUD_RUN_REGION` | Região do Cloud Run | `us-central1` |
| `_FORCE_BUILD_ALL` | (Opcional) Forçar build de tudo | Deixe vazio para detecção automática |

### Configuração Recomendada

1. **Branch**: Configure para executar em `main` ou `master`
2. **Substituições**: Configure todas as substituições acima
3. **Service Account**: Use uma conta de serviço com permissões:
   - Cloud Run Admin
   - Container Registry Service Agent
   - Cloud Build Service Account

## Logs e Debugging

O pipeline gera logs detalhados mostrando:

```
=== Flags de build ===
BUILD_WEB=true
BUILD_API=false
```

Você verá mensagens como:
- `✓ Mudanças detectadas em web/` - Build será executado
- `○ Sem mudanças em backend/` - Build será pulado
- `⚠ Mudanças em arquivos de configuração` - Build de tudo será executado

## Estratégias de Detecção

O sistema tenta encontrar o commit anterior usando estas estratégias (em ordem):

1. **HEAD~1** - Commit anterior direto
2. **origin/{BRANCH_NAME}** - Branch remota (se disponível)
3. **Última tag** - Tag mais recente do repositório
4. **Fallback** - Se nenhuma estratégia funcionar, faz build de tudo

## ⚠️ Importante: Variáveis NEXT_PUBLIC_*

**Variáveis que começam com `NEXT_PUBLIC_*` são embutidas no código durante o build**, não em runtime.

Isso significa que:
- ✅ Alterar `_NEXT_PUBLIC_API_BASE_URL` no trigger e fazer um novo build funciona
- ❌ Alterar `NEXT_PUBLIC_API_BASE_URL` no Cloud Run após o build **NÃO funciona**

**Para alterar a URL da API:**
1. Atualize `_NEXT_PUBLIC_API_BASE_URL` no trigger do Cloud Build
2. Force um novo build (faça um commit ou use `_FORCE_BUILD_ALL=true`)

Veja mais detalhes em [`web/VARIAVEIS_AMBIENTE_NEXTJS.md`](web/VARIAVEIS_AMBIENTE_NEXTJS.md).

## Limitações

- **Primeiro deploy**: Sempre faz build de tudo (não há histórico)
- **Mudanças em configuração**: Mudanças em `cloudbuild.yaml` sempre fazem build de tudo
- **Dependências**: Se a API mudar e a web depender dela, você pode precisar fazer build manual da web também
- **Variáveis NEXT_PUBLIC_***: Requerem rebuild para alterar (não podem ser alteradas em runtime)

## Troubleshooting

### Build sempre executa tudo

- Verifique se há histórico git suficiente
- Verifique se o trigger está configurado corretamente
- Veja os logs do step `detect-changes` para entender o que está acontecendo

### Imagem não encontrada ao pular build

- Isso é normal no primeiro deploy
- Em deploys subsequentes, o sistema tentará usar a última imagem disponível
- Se necessário, force o build completo usando `_FORCE_BUILD_ALL=true`

### Mudanças não detectadas

- Certifique-se de que os arquivos estão sendo commitados corretamente
- Verifique se o diretório está correto (`web/` ou `backend/`)
- Use `_FORCE_BUILD_ALL=true` se necessário

## Melhorias Futuras

Possíveis melhorias que podem ser implementadas:

- [ ] Cache de dependências entre builds
- [ ] Detecção de dependências entre projetos
- [ ] Build paralelo quando ambos precisam ser construídos
- [ ] Notificações quando builds são pulados



