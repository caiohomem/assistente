# Diagrama de Chamadas à AI no Sistema

## 📊 Resumo: Quantos Envios são Feitos à AI?

### Por Operação:

| Operação | Chamadas à OpenAI | Detalhes |
|----------|-------------------|----------|
| **Upload de Cartão de Visita** | **1 chamada** | OCR (Vision API) |
| **Processamento de Nota de Áudio** | **2 chamadas** | 1x Speech-to-Text + 1x LLM |
| **Text-to-Speech** (opcional) | **1 chamada** | TTS API (desabilitado por padrão) |

### Resumo Detalhado:

```
┌─────────────────────────────────────────────────────────────┐
│  CENÁRIO 1: Usuário faz upload de 1 cartão de visita       │
│  ────────────────────────────────────────────────────────  │
│  ✅ 1 chamada à OpenAI:                                     │
│     • OCR (Vision) - gpt-4o-mini                           │
│     • Usa: OcrPrompt                                        │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│  CENÁRIO 2: Usuário faz upload de 1 nota de áudio           │
│  ────────────────────────────────────────────────────────    │
│  ✅ 2 chamadas à OpenAI:                                    │
│     1. Speech-to-Text - whisper-1                           │
│        (não usa prompt)                                      │
│     2. LLM - gpt-4o-mini                                    │
│        (usa: TranscriptionPrompt)                           │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│  CENÁRIO 3: Usuário faz upload de 1 cartão + 1 áudio        │
│  ────────────────────────────────────────────────────────    │
│  ✅ 3 chamadas à OpenAI:                                    │
│     1. OCR (cartão)                                         │
│     2. Speech-to-Text (áudio)                               │
│     3. LLM (processamento do áudio)                         │
└─────────────────────────────────────────────────────────────┘
```

---

## 🎯 Visão Geral dos Fluxos

```
┌─────────────────────────────────────────────────────────────────┐
│                    FLUXO 1: CARTÃO DE VISITA                     │
│                                                                   │
│  Upload Imagem → OCR (Vision API) → Cria/Atualiza Contact        │
│                    ↑                                              │
│                    └── Usa: OcrPrompt                            │
│                                                                   │
│  ✅ 1 chamada à OpenAI                                            │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                    FLUXO 2: NOTA DE ÁUDIO                         │
│                                                                   │
│  Upload Áudio → Speech-to-Text (Whisper) → LLM (Chat API)        │
│                    ↑                    ↑                        │
│                    │                    └── Usa: TranscriptionPrompt
│                    └── Não usa prompt                             │
│                                                                   │
│  ✅ 2 chamadas à OpenAI                                           │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                    FLUXO 3: TEXT-TO-SPEECH (Opcional)            │
│                                                                   │
│  Texto → TTS API → Áudio MP3                                     │
│                                                                   │
│  ⚠️ Desabilitado por padrão                                      │
└─────────────────────────────────────────────────────────────────┘
```

---

## Fluxo 1: Processamento de Cartão de Visita (OCR)

```
┌─────────────────────────────────────────────────────────────┐
│                    FRONTEND                                  │
│  Upload de Imagem do Cartão de Visita                       │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│              CaptureController.UploadCard()                  │
│              POST /api/capture/upload-card                    │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│         UploadCardCommandHandler.Handle()                    │
│  1. Salva MediaAsset                                         │
│  2. Cria CaptureJob                                          │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│         OpenAIOcrProvider.ExtractFieldsAsync()                │
│  • Busca OcrPrompt da configuração (AgentConfiguration)      │
│  • Endpoint: POST /v1/chat/completions                       │
│  • Model: gpt-4o-mini (configurável)                        │
│  • Request: Imagem (base64) + Prompt                         │
│  • Response: JSON com campos extraídos                       │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│         UploadCardCommandHandler (continuação)                │
│  3. Processa resultado OCR                                   │
│  4. Cria/Atualiza Contact                                     │
│  5. Salva no banco                                           │
└─────────────────────────────────────────────────────────────┘
```

**Prompt usado:** `OcrPrompt` (da tabela `AgentConfigurations`)

---

## Fluxo 2: Processamento de Nota de Áudio

```
┌─────────────────────────────────────────────────────────────┐
│                    FRONTEND                                  │
│  Upload de Arquivo de Áudio                                 │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│         CaptureController.ProcessAudioNote()                 │
│         POST /api/capture/process-audio-note                 │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│      ProcessAudioNoteCommandHandler.Handle()                 │
│  1. Salva MediaAsset                                         │
│  2. Cria CaptureJob                                          │
│  3. Reserva créditos                                         │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│    CHAMADA 1: Speech-to-Text                                │
│    OpenAISpeechToTextProvider.TranscribeAsync()              │
│    • Endpoint: POST /v1/audio/transcriptions                 │
│    • Model: whisper-1                                        │
│    • Request: Arquivo de áudio (multipart/form-data)         │
│    • Response: Transcript (texto + segmentos)               │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│    CHAMADA 2: LLM Processing                                │
│    OpenAILLMProvider.SummarizeAndExtractTasksAsync()         │
│    • Busca TranscriptionPrompt da configuração              │
│    • Endpoint: POST /v1/chat/completions                     │
│    • Model: gpt-4o-mini (configurável)                      │
│    • Request: Prompt + Transcrição                           │
│    • Response: JSON com summary + suggestions                │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│      ProcessAudioNoteCommandHandler (continuação)            │
│  4. Processa resultado LLM                                   │
│  5. Cria ExtractedTasks                                      │
│  6. Salva no banco                                           │
│  7. Consome créditos reservados                              │
└─────────────────────────────────────────────────────────────┘
```

**Prompts usados:** 
- Nenhum para Speech-to-Text (Whisper não usa prompt)
- `TranscriptionPrompt` (da tabela `AgentConfigurations`) para LLM

---

## Fluxo 3: Text-to-Speech (Opcional - Desabilitado por padrão)

```
┌─────────────────────────────────────────────────────────────┐
│         OpenAITextToSpeechProvider.SynthesizeAsync()         │
│         • Endpoint: POST /v1/audio/speech                     │
│         • Model: tts-1                                       │
│         • Voice: nova (configurável)                         │
│         • Request: Texto para converter                      │
│         • Response: Arquivo de áudio (MP3)                   │
│                                                              │
│         ⚠️ ATENÇÃO: Desabilitado por padrão                  │
│         (OpenAI:TextToSpeech:Enabled = false)               │
└─────────────────────────────────────────────────────────────┘
```

---

## Resumo das Chamadas à OpenAI

| # | Serviço | Endpoint | Model | Prompt Configurável | Quando é Chamado |
|---|---------|----------|-------|---------------------|------------------|
| 1 | **OCR (Vision)** | `/v1/chat/completions` | `gpt-4o-mini` | ✅ `OcrPrompt` | Upload de cartão de visita |
| 2 | **Speech-to-Text** | `/v1/audio/transcriptions` | `whisper-1` | ❌ Não usa prompt | Processamento de nota de áudio |
| 3 | **LLM (Chat)** | `/v1/chat/completions` | `gpt-4o-mini` | ✅ `TranscriptionPrompt` | Após transcrição de áudio |
| 4 | **Text-to-Speech** | `/v1/audio/speech` | `tts-1` | ❌ Não usa prompt | Opcional (desabilitado) |

---

## Fluxo Completo: Nota de Áudio

```
Usuário faz upload de áudio
         │
         ▼
┌────────────────────┐
│ 1. Speech-to-Text  │  ← Chamada OpenAI #1 (Whisper)
│    (Whisper API)   │     Endpoint: /audio/transcriptions
└──────────┬─────────┘     Model: whisper-1
           │
           ▼
┌────────────────────┐
│ 2. LLM Processing  │  ← Chamada OpenAI #2 (Chat)
│    (Chat API)      │     Endpoint: /chat/completions
└──────────┬─────────┘     Model: gpt-4o-mini
           │               Prompt: TranscriptionPrompt
           ▼
    Salva resultado
```

**Total: 2 chamadas à OpenAI por nota de áudio**

---

## Fluxo Completo: Cartão de Visita

```
Usuário faz upload de imagem
         │
         ▼
┌────────────────────┐
│ 1. OCR (Vision)    │  ← Chamada OpenAI #1 (Vision)
│    (Chat API)      │     Endpoint: /chat/completions
└──────────┬─────────┘     Model: gpt-4o-mini
           │               Prompt: OcrPrompt
           │               Input: Imagem (base64) + Prompt
           ▼
    Cria/Atualiza Contact
```

**Total: 1 chamada à OpenAI por cartão de visita**

---

## Configuração dos Prompts

Os prompts são configuráveis através da tabela `AgentConfigurations`:

- **`OcrPrompt`**: Usado para extração de informações de cartões de visita
- **`TranscriptionPrompt`**: Usado para processamento de transcrições de áudio

Ambos podem ser editados através da interface web em `/configuracoes-agente`.

---

## Custos Estimados (OpenAI)

| Serviço | Model | Custo Aproximado |
|---------|-------|------------------|
| OCR (Vision) | gpt-4o-mini | ~$0.15 por 1M tokens (input) |
| Speech-to-Text | whisper-1 | $0.006 por minuto |
| LLM (Chat) | gpt-4o-mini | ~$0.15 por 1M tokens (input) |
| Text-to-Speech | tts-1 | $15.00 por 1M caracteres |

---

## Observações Importantes

1. **Speech-to-Text não usa prompt**: O Whisper API não aceita prompts customizados
2. **Text-to-Speech está desabilitado**: Por padrão, `OpenAI:TextToSpeech:Enabled = false`
3. **Prompts são dinâmicos**: Carregados do banco de dados em tempo de execução
4. **Fallback para prompts padrão**: Se não houver configuração no banco, usa prompts hardcoded

---

## 📋 Tabela de Referência Rápida

| # | Quando | Serviço OpenAI | Endpoint | Model | Prompt | Custo Aprox. |
|---|--------|----------------|----------|-------|--------|--------------|
| 1 | Upload cartão | **OCR (Vision)** | `/chat/completions` | `gpt-4o-mini` | `OcrPrompt` | $0.15/1M tokens |
| 2 | Upload áudio | **Speech-to-Text** | `/audio/transcriptions` | `whisper-1` | ❌ N/A | $0.006/min |
| 3 | Após transcrição | **LLM (Chat)** | `/chat/completions` | `gpt-4o-mini` | `TranscriptionPrompt` | $0.15/1M tokens |
| 4 | TTS (opcional) | **Text-to-Speech** | `/audio/speech` | `tts-1` | ❌ N/A | $15/1M chars |

---

## 🎯 Resposta Direta: Quantos Envios à AI?

**Resposta:** Depende da operação:

- **1 cartão de visita** = **1 envio** (OCR)
- **1 nota de áudio** = **2 envios** (Speech-to-Text + LLM)
- **1 cartão + 1 áudio** = **3 envios** (OCR + Speech-to-Text + LLM)

**Total de tipos de chamadas diferentes:** 3 (OCR, Speech-to-Text, LLM)

