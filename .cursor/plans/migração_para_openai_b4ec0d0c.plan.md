---
name: Migração para OpenAI
overview: Migrar os recursos de OCR, Speech-to-Text, Avaliação/LLM e adicionar Text-to-Speech para modelos da OpenAI, substituindo PaddleOCR, Whisper via Ollama/FastAPI e Qwen.
todos:
  - id: install-dependency
    content: Instalar pacote NuGet OpenAI versão 2.0.0 no projeto Infrastructure
    status: pending
  - id: create-text-to-speech-interface
    content: Criar interface ITextToSpeechProvider no Domain para converter texto em áudio
    status: pending
  - id: create-openai-ocr-provider
    content: Criar OpenAIOcrProvider implementando IOcrProvider usando gpt-4o-mini Vision API
    status: pending
    dependencies:
      - install-dependency
  - id: create-openai-speech-provider
    content: Criar OpenAISpeechToTextProvider implementando ISpeechToTextProvider usando whisper-1
    status: pending
    dependencies:
      - install-dependency
  - id: create-openai-llm-provider
    content: Criar OpenAILLMProvider implementando ILLMProvider usando gpt-4o-mini
    status: pending
    dependencies:
      - install-dependency
  - id: create-openai-tts-provider
    content: Criar OpenAITextToSpeechProvider implementando ITextToSpeechProvider usando tts-1 ou tts-1-hd
    status: pending
    dependencies:
      - install-dependency
      - create-text-to-speech-interface
  - id: integrate-tts-in-handler
    content: Integrar TTS no ProcessAudioNoteCommandHandler para gerar áudio de resposta após processamento
    status: pending
    dependencies:
      - create-openai-tts-provider
  - id: update-dependency-injection
    content: Atualizar DependencyInjection.cs para registrar os novos providers OpenAI nos switches de configuração
    status: pending
    dependencies:
      - create-openai-ocr-provider
      - create-openai-speech-provider
      - create-openai-llm-provider
      - create-openai-tts-provider
  - id: update-configuration
    content: Adicionar seção OpenAI no appsettings.json e documentar variáveis de ambiente no ENV_VARIABLES.md
    status: pending
  - id: implement-retry-logic
    content: Implementar retry com exponential backoff para lidar com rate limits da OpenAI
    status: pending
    dependencies:
      - create-openai-ocr-provider
      - create-openai-speech-provider
      - create-openai-llm-provider
      - create-openai-tts-provider
  - id: add-cost-logging
    content: Adicionar logging de custos por operação para monitoramento
    status: pending
    dependencies:
      - create-openai-ocr-provider
      - create-openai-speech-provider
      - create-openai-llm-provider
      - create-openai-tts-provider
---

#Migração para OpenAI - OCR, Speech-to-Text, LLM e Text-to-Speech

## Objetivo

Substituir os providers atuais (PaddleOCR, Whisper via Ollama/FastAPI, Qwen) por modelos da OpenAI e adicionar funcionalidade de Text-to-Speech para permitir que o assistente fale o resultado após processar áudio, aproveitando os 100 créditos/mês disponíveis.

## Modelos Recomendados

### OCR

- **Modelo**: `gpt-4o-mini` com Vision API
- **Custo**: ~$0.0001 por cartão de visita
- **Justificativa**: Melhor custo-benefício, alta precisão na extração de campos estruturados

### Speech-to-Text

- **Modelo**: `whisper-1` (Whisper API)
- **Custo**: $0.006 por minuto de áudio
- **Justificativa**: Modelo oficial otimizado, suporta português nativamente

### LLM/Avaliação

- **Modelo**: `gpt-4o-mini`
- **Custo**: ~$0.0006 por processamento de transcrição
- **Justificativa**: Custo baixo com qualidade adequada para resumos e extração de tarefas

### Text-to-Speech

- **Modelo**: `tts-1` (padrão) ou `tts-1-hd` (alta qualidade)
- **Custo**: $15 por 1M caracteres (~$0.000015 por caractere)
- **Vozes disponíveis**: alloy, echo, fable, onyx, nova, shimmer (recomendado: nova ou shimmer para português)
- **Justificativa**: Permite que o assistente fale o resultado do processamento, melhorando a experiência do usuário

## Estimativa de Custos

- **Uso médio**: ~$0.62/mês (50 OCR + 20 notas áudio + 20 LLM) + ~$0.03/mês (20 respostas TTS de 100 caracteres) = **~$0.65/mês**
- **Uso intensivo**: ~$6.08/mês (200 OCR + 100 notas áudio + 100 LLM) + ~$0.15/mês (100 respostas TTS de 100 caracteres) = **~$6.23/mês**
- **Conclusão**: Bem dentro dos $100 de créditos disponíveis

## Implementação

### 1. Dependências

- Instalar pacote NuGet `OpenAI` versão 2.0.0 em `AssistenteExecutivo.Infrastructure`

### 2. Novos Providers

#### 2.1. OpenAIOcrProvider

- **Arquivo**: `backend/src/AssistenteExecutivo.Infrastructure/Services/OpenAI/OpenAIOcrProvider.cs`
- **Interface**: Implementa `IOcrProvider`
- **Funcionalidade**: 
- Usa Vision API (`gpt-4o-mini`) para processar imagem do cartão
- Extrai campos estruturados (nome, email, telefone, empresa, cargo) via prompt JSON
- Retorna `OcrExtract` compatível com o sistema atual
- **Prompt**: Estruturado para garantir formato JSON consistente

#### 2.2. OpenAISpeechToTextProvider

- **Arquivo**: `backend/src/AssistenteExecutivo.Infrastructure/Services/OpenAI/OpenAISpeechToTextProvider.cs`
- **Interface**: Implementa `ISpeechToTextProvider`
- **Funcionalidade**:
- Usa Whisper API (`whisper-1`) para transcrever áudio
- Suporta múltiplos formatos (wav, mp3, m4a)
- Retorna `Transcript` com texto transcrito
- Idioma configurável (padrão: pt)

#### 2.3. OpenAILLMProvider

- **Arquivo**: `backend/src/AssistenteExecutivo.Infrastructure/Services/OpenAI/OpenAILLMProvider.cs`
- **Interface**: Implementa `ILLMProvider`
- **Funcionalidade**:
- Usa `gpt-4o-mini` para processar transcrições
- Extrai resumo e tarefas estruturadas
- Retorna `AudioProcessingResult` compatível
- Mantém formato JSON similar ao provider atual

#### 2.4. OpenAITextToSpeechProvider

- **Arquivo**: `backend/src/AssistenteExecutivo.Infrastructure/Services/OpenAI/OpenAITextToSpeechProvider.cs`
- **Interface**: Implementa `ITextToSpeechProvider` (nova interface no Domain)
- **Funcionalidade**:
- Usa TTS API (`tts-1` ou `tts-1-hd`) para converter texto em áudio
- Suporta múltiplas vozes (recomendado: nova ou shimmer para português)
- Retorna áudio em formato MP3 (padrão) ou outros formatos suportados
- Gera áudio com o resumo ou resposta baseada no processamento do LLM

### 3. Interface Text-to-Speech

#### 3.1. ITextToSpeechProvider

- **Arquivo**: `backend/src/AssistenteExecutivo.Domain/Interfaces/ITextToSpeechProvider.cs`
- **Método**: `Task<byte[]> SynthesizeAsync(string text, string voice, string format, CancellationToken cancellationToken)`
- **Parâmetros**:
  - `text`: Texto a ser convertido em áudio (ex: resumo gerado pelo LLM)
  - `voice`: Voz a ser usada (alloy, echo, fable, onyx, nova, shimmer)
  - `format`: Formato do áudio (mp3, opus, aac, flac)
  - `cancellationToken`: Token de cancelamento

### 4. Integração no Handler

#### 4.1. ProcessAudioNoteCommandHandler

- **Arquivo**: `backend/src/AssistenteExecutivo.Application/Handlers/Capture/ProcessAudioNoteCommandHandler.cs`
- **Modificações**:
  - Injetar `ITextToSpeechProvider` no construtor
  - Após processar com LLM, gerar áudio de resposta usando o resumo
  - Criar novo `MediaAsset` para o áudio gerado (tipo `MediaKind.Audio`)
  - Associar o áudio gerado à nota ou retornar no resultado
  - Adicionar ao `ProcessAudioNoteCommandResult` campo opcional `ResponseAudioId` ou `ResponseAudioUrl`

#### 4.2. ProcessAudioNoteCommandResult

- **Arquivo**: `backend/src/AssistenteExecutivo.Application/Commands/Capture/ProcessAudioNoteCommand.cs`
- **Modificações**:
  - Adicionar campo `ResponseMediaId?: Guid` para referenciar o áudio gerado
  - Adicionar campo `ResponseAudioUrl?: string` para URL de acesso ao áudio (opcional)

### 5. Cliente HTTP

- **Arquivo**: `backend/src/AssistenteExecutivo.Infrastructure/HttpClients/OpenAIClient.cs` (opcional)
- **Funcionalidade**: Wrapper para gerenciar requisições, retry com exponential backoff, tratamento de rate limits

### 6. Configuração

#### 6.1. appsettings.json

Adicionar seção `OpenAI`:

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
    },
    "TextToSpeech": {
      "Model": "tts-1",
      "Voice": "nova",
      "Format": "mp3",
      "Enabled": true
    }
  }
}
```

#### 6.2. Variáveis de Ambiente

Atualizar `ENV_VARIABLES.md` com:

- `OpenAI__ApiKey` (obrigatório)
- `OpenAI__OrganizationId` (opcional)
- `OpenAI__Ocr__Model`, `OpenAI__SpeechToText__Model`, `OpenAI__LLM__Model` (opcionais)
- `OpenAI__TextToSpeech__Model`, `OpenAI__TextToSpeech__Voice`, `OpenAI__TextToSpeech__Format`, `OpenAI__TextToSpeech__Enabled` (opcionais)

### 7. Dependency Injection

Atualizar `backend/src/AssistenteExecutivo.Infrastructure/DependencyInjection.cs`:

#### 7.1. OCR Provider

Adicionar case `"OpenAI"` no switch de `Ocr:Provider` (linha ~229)

#### 7.2. Speech-to-Text Provider

Adicionar case `"OpenAI"` no switch de `Whisper:Provider` (linha ~189)

#### 7.3. LLM Provider

Adicionar case `"OpenAI"` no switch de `Ollama:LLM:Provider` (linha ~268)

#### 7.4. Text-to-Speech Provider

Adicionar novo switch para `TextToSpeech:Provider`:

- Registrar `OpenAITextToSpeechProvider` quando `TextToSpeech:Provider` for `"OpenAI"`
- Criar `StubTextToSpeechProvider` para desenvolvimento/testes
- Provider opcional (pode ser desabilitado via configuração)

### 8. Refinamento OCR (Opcional)

- Avaliar se `QwenOcrRefinementService` ainda é necessário
- Se OpenAI OCR for preciso o suficiente, pode remover ou tornar opcional
- Manter como fallback inicialmente

## Estratégia de Migração

### Fase 1: Desenvolvimento

1. Implementar providers em ambiente local
2. Testar com dados reais
3. Validar qualidade e custos

### Fase 2: Staging

1. Deploy com OpenAI habilitado
2. Testes com usuários internos
3. Monitorar custos e performance

### Fase 3: Produção

1. Migração via configuração (alterar `Ocr:Provider`, `Whisper:Provider`, `Ollama:LLM:Provider` para `"OpenAI"`)
2. Manter providers antigos como fallback opcional
3. Monitoramento ativo de erros e custos

## Considerações Importantes

### Rate Limits

- Implementar retry automático com exponential backoff
- GPT-4o-mini: 10M tokens/minuto (Tier 1)
- Whisper: Sem limite específico, mas implementar retry

### Segurança

- API Key nunca deve ser commitada
- Usar Secret Manager ou variáveis de ambiente
- Não logar conteúdo completo de requisições/respostas

### Custos

- Implementar logging de custos por operação
- Configurar alertas se custo mensal exceder threshold
- Monitorar uso via dashboard da OpenAI

### Compatibilidade

- Manter interfaces existentes (`IOcrProvider`, `ISpeechToTextProvider`, `ILLMProvider`)
- Criar nova interface `ITextToSpeechProvider` no Domain
- Retornar objetos de domínio existentes (`OcrExtract`, `Transcript`, `AudioProcessingResult`)
- Não quebrar contratos com handlers existentes
- TTS é opcional (pode ser desabilitado via configuração)

### Fluxo de Text-to-Speech

1. Usuário envia áudio (nota de áudio)
2. Sistema transcreve áudio (Speech-to-Text)
3. Sistema processa transcrição (LLM) gerando resumo e tarefas
4. **NOVO**: Sistema gera áudio de resposta usando TTS com o resumo
5. Sistema retorna resultado incluindo:

   - Transcrição original
   - Resumo e tarefas
   - **NOVO**: Áudio de resposta (opcional, se TTS estiver habilitado)

## Arquivos a Modificar/Criar

### Novos Arquivos

- `backend/src/AssistenteExecutivo.Domain/Interfaces/ITextToSpeechProvider.cs` (nova interface)
- `backend/src/AssistenteExecutivo.Infrastructure/Services/OpenAI/OpenAIOcrProvider.cs`
- `backend/src/AssistenteExecutivo.Infrastructure/Services/OpenAI/OpenAISpeechToTextProvider.cs`
- `backend/src/AssistenteExecutivo.Infrastructure/Services/OpenAI/OpenAILLMProvider.cs`
- `backend/src/AssistenteExecutivo.Infrastructure/Services/OpenAI/OpenAITextToSpeechProvider.cs`
- `backend/src/AssistenteExecutivo.Infrastructure/Services/StubTextToSpeechProvider.cs` (para testes)
- `backend/src/AssistenteExecutivo.Infrastructure/HttpClients/OpenAIClient.cs` (opcional)

### Arquivos a Modificar

- `backend/src/AssistenteExecutivo.Infrastructure/AssistenteExecutivo.Infrastructure.csproj` (adicionar pacote)
- `backend/src/AssistenteExecutivo.Infrastructure/DependencyInjection.cs` (registrar providers, incluindo TTS)
- `backend/src/AssistenteExecutivo.Application/Handlers/Capture/ProcessAudioNoteCommandHandler.cs` (integrar TTS)
- `backend/src/AssistenteExecutivo.Application/Commands/Capture/ProcessAudioNoteCommand.cs` (adicionar campo de resposta de áudio)
- `backend/src/AssistenteExecutivo.Api/appsettings.json` (adicionar configuração TTS)
- `ENV_VARIABLES.md` (documentar variáveis TTS)

## Testes

### Testes Unitários

- Mock da API OpenAI
- Testar casos de erro e fallback
- Validar parsing de respostas

### Testes de Integração

- Testar com imagens reais de cartões
- Testar com áudios reais
- Validar qualidade das extrações

### Testes de Performance

- Medir latência das chamadas