# Como Testar o Servidor OCR

## 1. Verificar se o container está rodando

```bash
docker-compose -f ../docker/docker-compose.keycloak.yml ps ocr-api
```

Ou verificar os logs:

```bash
docker-compose -f ../docker/docker-compose.keycloak.yml logs ocr-api
```

## 2. Testar o endpoint de saúde (se disponível)

O servidor FastAPI geralmente expõe automaticamente `/docs` para documentação interativa:

```bash
# Abrir no navegador
http://localhost:8000/docs
```

## 3. Testar o endpoint `/ocr`

### Usando PowerShell (Windows)

```powershell
# Testar OCR com uma imagem
$imagePath = "caminho/para/sua/imagem.png"
$uri = "http://localhost:8000/ocr"

$form = @{
    file = Get-Item -Path $imagePath
    lang = "pt"
    debug = "false"
}

Invoke-RestMethod -Uri $uri -Method Post -Form $form
```

### Usando curl (Linux/Mac/Git Bash)

```bash
# Testar OCR com uma imagem
curl -X POST "http://localhost:8000/ocr" \
  -F "file=@/caminho/para/sua/imagem.png" \
  -F "lang=pt" \
  -F "debug=false"
```

### Usando Python

```python
import requests

url = "http://localhost:8000/ocr"

with open("caminho/para/sua/imagem.png", "rb") as f:
    files = {"file": f}
    data = {"lang": "pt", "debug": "false"}
    response = requests.post(url, files=files, data=data)
    print(response.json())
```

## 4. Testar o endpoint `/transcribe`

### Usando PowerShell (Windows)

```powershell
# Testar transcrição de áudio
$audioPath = "caminho/para/seu/audio.wav"
$uri = "http://localhost:8000/transcribe"

$form = @{
    file = Get-Item -Path $audioPath
    language = "pt"
}

Invoke-RestMethod -Uri $uri -Method Post -Form $form
```

### Usando curl

```bash
# Testar transcrição de áudio
curl -X POST "http://localhost:8000/transcribe" \
  -F "file=@/caminho/para/seu/audio.wav" \
  -F "language=pt"
```

### Usando Python

```python
import requests

url = "http://localhost:8000/transcribe"

with open("caminho/para/seu/audio.wav", "rb") as f:
    files = {"file": f}
    data = {"language": "pt"}
    response = requests.post(url, files=files, data=data)
    print(response.json())
```

## 5. Exemplo de resposta esperada

### Resposta do `/ocr`:
```json
{
  "rawText": "Texto extraído da imagem",
  "lines": [
    {
      "text": "Linha 1",
      "confidence": 0.95
    },
    {
      "text": "Linha 2",
      "confidence": 0.87
    }
  ],
  "lang": "pt"
}
```

### Resposta do `/transcribe`:
```json
{
  "language": "pt",
  "duration": 10.5,
  "text": "Texto transcrito do áudio"
}
```

## 6. Teste rápido com imagem de exemplo

Se você tiver uma imagem de teste, pode usar este comando PowerShell:

```powershell
# Criar um arquivo de teste simples (opcional)
$testImage = "test.png"
# ... criar ou usar uma imagem existente ...

# Testar
$response = Invoke-RestMethod -Uri "http://localhost:8000/ocr" -Method Post -Form @{
    file = Get-Item -Path $testImage
    lang = "pt"
}
$response | ConvertTo-Json -Depth 10
```

## 7. Verificar logs em tempo real

Para ver os logs do container enquanto testa:

```bash
docker-compose -f ../docker/docker-compose.keycloak.yml logs -f ocr-api
```

## 8. Testar com modo debug (OCR)

Para obter informações detalhadas sobre o processamento:

```bash
curl -X POST "http://localhost:8000/ocr" \
  -F "file=@imagem.png" \
  -F "lang=pt" \
  -F "debug=true"
```

Isso retornará informações adicionais como tamanho da imagem, tipo de resultado, etc.




