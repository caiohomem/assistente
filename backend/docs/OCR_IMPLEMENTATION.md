# Implementação de OCR para Leitura de Cartões de Visita

Este documento descreve como implementar um provedor OCR real para substituir o `StubOcrProvider` atual.

## Estado Atual

Atualmente, o sistema usa `StubOcrProvider`, que retorna dados de exemplo para permitir testes sem um serviço OCR real. O stub retorna:
- Nome: "João Silva"
- Email: "joao.silva@exemplo.com"
- Telefone: "+55 11 98765-4321"
- Empresa: "Empresa Exemplo Ltda"
- Cargo: "Gerente de Vendas"

## Configuração

A configuração do OCR está no `appsettings.json`:

```json
{
  "Ocr": {
    "Provider": "Stub",
    "Azure": {
      "Endpoint": "",
      "ApiKey": "",
      "ApiVersion": "2024-02-01"
    },
    "GoogleCloud": {
      "ProjectId": "",
      "CredentialsJson": ""
    },
    "Aws": {
      "Region": "",
      "AccessKeyId": "",
      "SecretAccessKey": ""
    }
  }
}
```

O valor de `Ocr:Provider` determina qual provider será usado:
- `"Stub"` - Provider de exemplo (padrão)
- `"Azure"` - Azure Computer Vision
- `"GoogleCloud"` - Google Cloud Vision API
- `"Aws"` - AWS Textract

## Como Implementar um Provider Real

### 1. Criar a Classe do Provider

Crie uma nova classe implementando `IOcrProvider`:

```csharp
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AssistenteExecutivo.Infrastructure.Services;

public class AzureComputerVisionOcrProvider : IOcrProvider
{
    private readonly ILogger<AzureComputerVisionOcrProvider> _logger;
    private readonly string _apiEndpoint;
    private readonly string _apiKey;

    public AzureComputerVisionOcrProvider(
        IConfiguration configuration,
        ILogger<AzureComputerVisionOcrProvider> logger)
    {
        _logger = logger;
        _apiEndpoint = configuration["Ocr:Azure:Endpoint"] 
            ?? throw new InvalidOperationException("Ocr:Azure:Endpoint não configurado");
        _apiKey = configuration["Ocr:Azure:ApiKey"] 
            ?? throw new InvalidOperationException("Ocr:Azure:ApiKey não configurado");
    }

    public async Task<OcrExtract> ExtractFieldsAsync(
        byte[] imageBytes, 
        string mimeType, 
        CancellationToken cancellationToken = default)
    {
        // Implementar chamada ao serviço OCR
        // Parsear resposta e retornar OcrExtract
    }
}
```

### 2. Instalar Pacotes NuGet Necessários

#### Para Azure Computer Vision:
```bash
dotnet add package Microsoft.Azure.CognitiveServices.Vision.ComputerVision
```

#### Para Google Cloud Vision:
```bash
dotnet add package Google.Cloud.Vision.V1
```

#### Para AWS Textract:
```bash
dotnet add package AWSSDK.Textract
```

### 3. Atualizar DependencyInjection.cs

No arquivo `DependencyInjection.cs`, atualize o switch case para registrar seu provider:

```csharp
case "Azure":
    var azureEndpoint = configuration["Ocr:Azure:Endpoint"];
    var azureApiKey = configuration["Ocr:Azure:ApiKey"];
    if (string.IsNullOrEmpty(azureEndpoint) || string.IsNullOrEmpty(azureApiKey))
    {
        throw new InvalidOperationException(
            "Azure OCR configurado mas Endpoint ou ApiKey não fornecidos");
    }
    services.AddScoped<IOcrProvider, AzureComputerVisionOcrProvider>();
    break;
```

### 4. Configurar Credenciais

Adicione as credenciais no `appsettings.Development.json` ou variáveis de ambiente:

```json
{
  "Ocr": {
    "Provider": "Azure",
    "Azure": {
      "Endpoint": "https://your-resource.cognitiveservices.azure.com/",
      "ApiKey": "your-api-key-here"
    }
  }
}
```

## Estrutura do OcrExtract

O `OcrExtract` espera os seguintes campos:

```csharp
public sealed class OcrExtract : ValueObject
{
    public string? Name { get; }
    public string? Email { get; }
    public string? Phone { get; }
    public string? Company { get; }
    public string? JobTitle { get; }
    public Dictionary<string, decimal> ConfidenceScores { get; }
}
```

**Importante**: O contato deve ter pelo menos um email ou telefone (`HasMinimumData`). Se o OCR não extrair nenhum desses campos, a criação do contato falhará.

## Extração de Campos

A extração de campos específicos (nome, email, telefone, etc.) a partir do texto OCR geralmente requer:

1. **Regex Patterns**: Para identificar emails e telefones
2. **Heurísticas**: Para identificar nome (geralmente primeira linha), empresa, cargo
3. **ML/NLP**: Para melhor precisão (opcional, mas recomendado)

### Exemplo de Regex:

```csharp
// Email
var emailPattern = @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b";
var emailMatch = Regex.Match(ocrText, emailPattern, RegexOptions.IgnoreCase);

// Telefone (formato brasileiro)
var phonePattern = @"(\+55\s?)?(\(?\d{2}\)?\s?)?(\d{4,5}[-.\s]?\d{4})";
var phoneMatch = Regex.Match(ocrText, phonePattern);
```

## Exemplo de Implementação Completa

Veja o arquivo `OcrProviderExample.cs` para um exemplo completo de estrutura.

## Testes

Após implementar, teste com:
1. Imagens de cartões de visita reais
2. Diferentes formatos (JPEG, PNG, WebP)
3. Diferentes qualidades de imagem
4. Cartões com diferentes layouts

## Próximos Passos

1. Escolher o provedor OCR (Azure, Google Cloud, AWS, ou outro)
2. Criar a classe do provider
3. Implementar a lógica de extração
4. Configurar credenciais
5. Atualizar `DependencyInjection.cs`
6. Testar com dados reais
7. Remover ou manter `StubOcrProvider` para desenvolvimento




