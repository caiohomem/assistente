# Plano de Migração para OpenAI

## 📋 Resumo Executivo

Este documento descreve o plano para migrar os recursos de **OCR**, **Speech-to-Text** e **Avaliação/LLM** do sistema atual (PaddleOCR, Whisper via Ollama/FastAPI, Qwen) para modelos da **OpenAI**.

## 🎯 Modelos OpenAI Recomendados

### 1. OCR (Reconhecimento de Texto em Imagens)
**Modelo Recomendado:** `gpt-4o-mini` com Vision API

**Justificativa:**
- ✅ Excelente para extração estruturada de informações de cartões de visita
- ✅ Custo-benefício superior: ~$0.15 por 1M tokens de input (imagens são processadas como tokens)
- ✅ Alta precisão na extração de campos (nome, email, telefone, empresa, cargo)
- ✅ Suporte nativo a português
- ✅ Pode processar imagens diretamente sem OCR prévio

**Alternativa:** `gpt-4o` (se precisar de maior precisão, ~$2.50 por 1M tokens)

**Custo Estimado:**
- Cartão de visita típico: ~500-1000 tokens (incluindo imagem)
- **gpt-4o-mini**: ~$0.000075 - $0.00015 por cartão
- **gpt-4o**: ~$0.00125 - $0.0025 por cartão

### 2. Speech-to-Text (Transcrição de Áudio)
**Modelo Recomendado:** `whisper-1` (Whisper API)

**Justificativa:**
- ✅ Modelo oficial da OpenAI, otimizado para transcrição
- ✅ Suporte a múltiplos idiomas (incluindo português)
- ✅ Alta precisão mesmo com áudio de qualidade variável
- ✅ Custo fixo por minuto: $0.006 por minuto de áudio
- ✅ Suporta arquivos até 25MB

**Custo Estimado:**
- Nota de áudio de 1 minuto: $0.006
- Nota de áudio de 5 minutos: $0.03
- Nota de áudio de 10 minutos: $0.06

### 3. Avaliação/LLM (Processamento de Texto)
**Modelo Recomendado:** `gpt-4o-mini`

**Justificativa:**
- ✅ Excelente para tarefas de resumo e extração de informações
- ✅ Custo muito baixo: $0.15 por 1M tokens input, $0.60 por 1M tokens output
- ✅ Boa qualidade para processamento de transcrições
- ✅ Resposta rápida

**Alternativa:** `gpt-4o` (se precisar de maior qualidade/raciocínio)

**Custo Estimado:**
- Transcrição de 1000 palavras (~1300 tokens): ~$0.0002 (input) + ~$0.0004 (output) = **$0.0006**
- Transcrição de 5000 palavras (~6500 tokens): ~$0.001 (input) + ~$0.0024 (output) = **$0.0034**

## 💰 Estimativa de Custos Mensais (100 créditos/mês)

### Cenário Conservador (uso médio)
- **OCR**: 50 cartões/mês × $0.0001 = **$0.005**
- **Speech-to-Text**: 20 notas de 5min/mês × $0.03 = **$0.60**
- **LLM**: 20 processamentos × $0.0006 = **$0.012**

**Total: ~$0.62/mês** (bem dentro dos $100 de créditos)

### Cenário Intensivo
- **OCR**: 200 cartões/mês × $0.0001 = **$0.02**
- **Speech-to-Text**: 100 notas de 10min/mês × $0.06 = **$6.00**
- **LLM**: 100 processamentos × $0.0006 = **$0.06**

**Total: ~$6.08/mês** (ainda muito abaixo dos $100)

## 📦 Dependências Necessárias

### NuGet Packages
```xml
<!-- OpenAI SDK para .NET -->
<PackageReference Include="OpenAI" Version="2.0.0" />
<!-- ou -->
<PackageReference Include="Betalgo.OpenAI" Version="8.0.0" />
```

## 🏗️ Arquitetura da Migração

### Estrutura de Providers

```
AssistenteExecutivo.Infrastructure/
├── Services/
│   ├── OpenAI/
│   │   ├── OpenAIOcrProvider.cs          # Novo: OCR com GPT-4o-mini Vision
│   │   ├── OpenAISpeechToTextProvider.cs # Novo: Whisper API
│   │   └── OpenAILLMProvider.cs          # Novo: GPT-4o-mini para processamento
│   └── ... (providers existentes mantidos)
├── HttpClients/
│   └── OpenAIClient.cs                   # Novo: Cliente HTTP para OpenAI
└── DependencyInjection.cs                # Atualizar registros
```

## 📝 Plano de Implementação

### Fase 1: Preparação e Configuração (1-2 horas)

#### 1.1. Instalar Dependências
```bash
cd backend/src/AssistenteExecutivo.Infrastructure
dotnet add package OpenAI --version 2.0.0
```

#### 1.2. Adicionar Configuração
Atualizar `appsettings.json`:
```json
{
  "OpenAI": {
    "ApiKey": "",
    "OrganizationId": "",
    "Ocr": {
      "Model": "gpt-4o-mini",
      "Temperature": "0.0",
      "MaxTokens": "500"
    },
    "SpeechToText": {
      "Model": "whisper-1",
      "Language": "pt"
    },
    "LLM": {
      "Model": "gpt-4o-mini",
      "Temperature": "0.3",
      "MaxTokens": "2000"
    }
  }
}
```

Atualizar `ENV_VARIABLES.md` com as novas variáveis:
```bash
OpenAI__ApiKey="sk-..."
OpenAI__OrganizationId=""  # Opcional
OpenAI__Ocr__Model="gpt-4o-mini"
OpenAI__SpeechToText__Model="whisper-1"
OpenAI__LLM__Model="gpt-4o-mini"
```

### Fase 2: Implementação dos Providers (4-6 horas)

#### 2.1. Criar OpenAIClient (HttpClient Wrapper)
- Classe para gerenciar requisições à API OpenAI
- Tratamento de erros e rate limiting
- Suporte a retry automático

#### 2.2. Implementar OpenAIOcrProvider
- Implementar `IOcrProvider`
- Usar Vision API para processar imagem
- Extrair campos estruturados (nome, email, telefone, empresa, cargo)
- Usar prompt estruturado para garantir formato JSON consistente
- Fallback para heurísticas se necessário

**Prompt Sugerido:**
```
Analise esta imagem de um cartão de visita e extraia as seguintes informações em formato JSON:
{
  "name": "nome completo da pessoa",
  "email": "endereço de email",
  "phone": "telefone (formato brasileiro)",
  "company": "nome da empresa",
  "jobTitle": "cargo/função"
}

Extraia apenas informações que estejam claramente visíveis na imagem. Se algum campo não estiver presente, use null.
```

#### 2.3. Implementar OpenAISpeechToTextProvider
- Implementar `ISpeechToTextProvider`
- Usar Whisper API para transcrever áudio
- Suportar múltiplos formatos (wav, mp3, m4a, etc.)
- Retornar `Transcript` com texto transcrito

#### 2.4. Implementar OpenAILLMProvider
- Implementar `ILLMProvider`
- Usar GPT-4o-mini para processar transcrições
- Extrair resumo e tarefas estruturadas
- Manter compatibilidade com formato `AudioProcessingResult` existente

**Prompt Sugerido (similar ao atual):**
```
Analise a seguinte transcrição de uma nota de áudio sobre um contato e organize as informações de forma estruturada.

TRANSCRIÇÃO:
{transcript}

Extraia e organize as informações em formato JSON válido com a seguinte estrutura:
{
  "summary": "resumo conciso em 2-3 frases do conteúdo principal",
  "suggestions": [
    "sugestão de ação 1",
    "sugestão de ação 2"
  ]
}
```

### Fase 3: Atualização de Dependency Injection (1 hora)

#### 3.1. Atualizar DependencyInjection.cs
- Adicionar opções de configuração para OpenAI
- Registrar providers baseado em configuração
- Manter providers antigos como fallback opcional

**Estrutura de Configuração:**
```csharp
// OCR Provider
var ocrProvider = configuration["Ocr:Provider"] ?? "Stub";
switch (ocrProvider)
{
    case "OpenAI":
        services.AddScoped<IOcrProvider, OpenAIOcrProvider>();
        break;
    case "PaddleOcr":
        // ... existente
        break;
    // ... outros
}

// Speech-to-Text Provider
var speechToTextProvider = configuration["Whisper:Provider"] ?? "Stub";
switch (speechToTextProvider)
{
    case "OpenAI":
        services.AddScoped<ISpeechToTextProvider, OpenAISpeechToTextProvider>();
        break;
    // ... existente
}

// LLM Provider
var llmProvider = configuration["Ollama:LLM:Provider"] ?? "Stub";
switch (llmProvider)
{
    case "OpenAI":
        services.AddScoped<ILLMProvider, OpenAILLMProvider>();
        break;
    // ... existente
}
```

### Fase 4: Testes (2-3 horas)

#### 4.1. Testes Unitários
- Criar testes para cada provider
- Mock da API OpenAI
- Testar casos de erro e fallback

#### 4.2. Testes de Integração
- Testar com imagens reais de cartões
- Testar com áudios reais
- Validar qualidade das extrações

#### 4.3. Testes de Performance
- Medir latência das chamadas
- Validar rate limiting
- Testar com múltiplas requisições simultâneas

### Fase 5: Migração Gradual (1-2 semanas)

#### 5.1. Ambiente de Desenvolvimento
- Configurar OpenAI em dev
- Testar todos os fluxos
- Validar custos

#### 5.2. Ambiente de Staging
- Deploy com OpenAI habilitado
- Testes com usuários internos
- Monitorar custos e performance

#### 5.3. Produção (Feature Flag)
- Implementar feature flag para alternar entre providers
- Migração gradual por usuário ou funcionalidade
- Monitoramento ativo de erros e custos

## 🔄 Estratégia de Rollback

### Opções de Rollback
1. **Via Configuração**: Alterar `Ocr:Provider`, `Whisper:Provider`, `Ollama:LLM:Provider` para valores antigos
2. **Feature Flag**: Desabilitar OpenAI via feature flag sem necessidade de deploy
3. **Fallback Automático**: Implementar fallback para providers antigos em caso de erro

### Monitoramento
- Logs de erros da API OpenAI
- Métricas de custo por requisição
- Taxa de sucesso vs. providers antigos
- Latência comparativa

## 📊 Métricas de Sucesso

### KPIs a Monitorar
1. **Precisão OCR**: Taxa de campos extraídos corretamente
2. **Precisão Speech-to-Text**: WER (Word Error Rate) comparado ao baseline
3. **Qualidade LLM**: Satisfação com resumos e tarefas extraídas
4. **Custo**: Custo médio por operação
5. **Latência**: Tempo de resposta médio
6. **Disponibilidade**: Taxa de sucesso das chamadas

## 🚨 Considerações Importantes

### Rate Limits da OpenAI
- **GPT-4o-mini**: 10M tokens/minuto (Tier 1)
- **Whisper**: Sem limite específico documentado, mas recomenda-se implementar retry com backoff
- **Implementar**: Retry automático com exponential backoff

### Segurança
- **Nunca commitar API Key**: Usar Secret Manager ou variáveis de ambiente
- **Validação de Input**: Validar tamanho de arquivos antes de enviar
- **Logging**: Não logar conteúdo completo de requisições/respostas (apenas metadados)

### Custos
- **Monitoramento**: Implementar logging de custos por operação
- **Alertas**: Configurar alertas se custo mensal exceder threshold
- **Otimização**: Usar `gpt-4o-mini` sempre que possível (custo 10x menor que `gpt-4o`)

## 📚 Referências

- [OpenAI API Documentation](https://platform.openai.com/docs)
- [OpenAI .NET SDK](https://github.com/OpenAI/OpenAI-DotNet)
- [OpenAI Pricing](https://openai.com/api/pricing/)
- [Whisper API](https://platform.openai.com/docs/guides/speech-to-text)
- [Vision API](https://platform.openai.com/docs/guides/vision)

## ✅ Checklist de Implementação

### Preparação
- [ ] Obter API Key da OpenAI
- [ ] Instalar pacote NuGet OpenAI
- [ ] Adicionar configurações ao appsettings.json
- [ ] Atualizar ENV_VARIABLES.md

### Implementação
- [ ] Criar OpenAIClient
- [ ] Implementar OpenAIOcrProvider
- [ ] Implementar OpenAISpeechToTextProvider
- [ ] Implementar OpenAILLMProvider
- [ ] Atualizar DependencyInjection.cs

### Testes
- [ ] Testes unitários para cada provider
- [ ] Testes de integração
- [ ] Testes de performance
- [ ] Validação com dados reais

### Deploy
- [ ] Configurar variáveis de ambiente em dev
- [ ] Testar em ambiente de desenvolvimento
- [ ] Deploy em staging
- [ ] Testes em staging
- [ ] Deploy em produção (com feature flag)
- [ ] Monitoramento ativo

### Documentação
- [ ] Atualizar README com instruções de configuração
- [ ] Documentar custos esperados
- [ ] Criar guia de troubleshooting



