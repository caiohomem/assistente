# Variáveis de Ambiente no Next.js - Build Time vs Runtime

## ⚠️ Problema Comum

Variáveis que começam com `NEXT_PUBLIC_*` são **embutidas no código JavaScript durante o build**, não em runtime. Isso significa:

- ✅ **Funciona**: Alterar a variável e fazer um novo build
- ❌ **NÃO funciona**: Alterar a variável no Cloud Run após o build já estar feito

## Como Funciona

### Build Time (NEXT_PUBLIC_*)

Variáveis `NEXT_PUBLIC_*` são substituídas no código durante `npm run build`:

```typescript
// Durante o build, isso:
const apiUrl = process.env.NEXT_PUBLIC_API_BASE_URL;

// Vira isso no código final:
const apiUrl = "https://api.assistente.live/";
```

**Consequência**: O valor fica "hardcoded" no JavaScript gerado. Alterar a variável no Cloud Run não tem efeito.

### Runtime (sem NEXT_PUBLIC_)

Variáveis sem o prefixo `NEXT_PUBLIC_` só estão disponíveis no servidor (Server Components, API Routes) e podem ser alteradas em runtime.

## Solução: Rebuild Necessário

Para alterar `NEXT_PUBLIC_API_BASE_URL` em produção:

### Opção 1: Via Cloud Build (Recomendado)

1. **Configure a substituição no Trigger do Cloud Build**:
   - Vá para **Cloud Build > Triggers**
   - Edite o trigger
   - Configure `_NEXT_PUBLIC_API_BASE_URL` com o novo valor
   - Exemplo: `https://api.assistente.live/`

2. **Force um novo build**:
   ```bash
   # Via gcloud CLI
   gcloud builds submit --config=cloudbuild.yaml \
     --substitutions=_FORCE_BUILD_ALL=true,_NEXT_PUBLIC_API_BASE_URL=https://api.assistente.live/
   ```

   Ou simplesmente faça um commit (mesmo que vazio) para disparar o trigger:
   ```bash
   git commit --allow-empty -m "Rebuild com nova URL da API"
   git push
   ```

### Opção 2: Build Manual Local

```bash
cd web

# Build com a nova URL
docker build \
  -f Dockerfile \
  --target runner \
  --build-arg NEXT_PUBLIC_API_BASE_URL=https://api.assistente.live/ \
  -t gcr.io/SEU_PROJECT_ID/assistente-web:latest \
  .

# Push para o registry
docker push gcr.io/SEU_PROJECT_ID/assistente-web:latest

# Deploy no Cloud Run
gcloud run deploy assistente-web \
  --image gcr.io/SEU_PROJECT_ID/assistente-web:latest \
  --region us-central1 \
  --platform managed
```

## Verificação

Após o rebuild, você pode verificar se a variável foi aplicada corretamente:

1. **Inspecionar o código gerado**:
   - Abra o DevTools no navegador
   - Vá para Sources > .next > static
   - Procure por arquivos `.js` e verifique se contém a URL correta

2. **Verificar no código fonte**:
   ```bash
   # Dentro do container ou após build local
   grep -r "api.assistente.live" .next/
   ```

## Por Que Isso Acontece?

O Next.js faz isso por design:

- **Performance**: Variáveis públicas são conhecidas em build time, permitindo otimizações
- **Segurança**: Evita expor variáveis sensíveis no cliente
- **Bundling**: Permite tree-shaking e outras otimizações

## Alternativa: Variáveis Runtime (Avançado)

Se você realmente precisa de variáveis runtime no cliente, você pode:

1. **Criar uma API Route** que retorna as configurações:
   ```typescript
   // app/api/config/route.ts
   export async function GET() {
     return Response.json({
       apiBaseUrl: process.env.API_BASE_URL // Sem NEXT_PUBLIC_
     });
   }
   ```

2. **Buscar no cliente**:
   ```typescript
   const config = await fetch('/api/config').then(r => r.json());
   const apiUrl = config.apiBaseUrl;
   ```

**Desvantagem**: Requer uma requisição adicional no carregamento inicial.

## Checklist

- [ ] Entendi que `NEXT_PUBLIC_*` precisa de rebuild
- [ ] Configurei `_NEXT_PUBLIC_API_BASE_URL` no trigger do Cloud Build
- [ ] Fiz um novo build após alterar a variável
- [ ] Verifiquei que a nova URL está no código gerado

## Referências

- [Next.js Environment Variables](https://nextjs.org/docs/app/building-your-application/configuring/environment-variables)
- [Next.js Public Runtime Config (deprecated)](https://nextjs.org/docs/pages/api-reference/next-config-js/runtime-config)

