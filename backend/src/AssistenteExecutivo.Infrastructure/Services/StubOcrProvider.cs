using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace AssistenteExecutivo.Infrastructure.Services;

/// <summary>
/// Stub implementation of IOcrProvider.
/// This is a placeholder that returns sample OCR extracts for testing purposes.
/// 
/// IMPORTANT: Replace with actual implementation when ready.
/// 
/// To implement a real OCR provider:
/// 1. Create a new class implementing IOcrProvider (e.g., AzureComputerVisionOcrProvider)
/// 2. Implement ExtractFieldsAsync to call your OCR service (Azure, Google Cloud Vision, AWS Textract, etc.)
/// 3. Parse the OCR response and map to OcrExtract
/// 4. Update DependencyInjection.cs to register the new provider instead of StubOcrProvider
/// 
/// Example providers:
/// - Azure Computer Vision: https://learn.microsoft.com/en-us/azure/ai-services/computer-vision/
/// - Google Cloud Vision API: https://cloud.google.com/vision/docs
/// - AWS Textract: https://aws.amazon.com/textract/
/// - Tesseract OCR: https://github.com/tesseract-ocr/tesseract
/// </summary>
public class StubOcrProvider : IOcrProvider
{
    private readonly ILogger<StubOcrProvider>? _logger;

    public StubOcrProvider(ILogger<StubOcrProvider>? logger = null)
    {
        _logger = logger;
    }

    public Task<OcrExtract> ExtractFieldsAsync(byte[] imageBytes, string mimeType, CancellationToken cancellationToken = default)
    {
        _logger?.LogWarning(
            "StubOcrProvider is being used. This returns sample data. " +
            "Implement a real OCR provider (Azure Computer Vision, Google Cloud Vision, etc.) for production.");

        // Stub implementation: returns sample OCR extract for testing
        // This ensures the validation passes (requires email or phone)
        // Phone must be 10 or 11 digits after removing formatting (Brazilian format)
        var sampleExtract = new OcrExtract(
            name: "João Silva",
            email: "joao.silva@exemplo.com",
            phone: "(11) 98765-4321", // 11 digits: 11987654321
            company: "Empresa Exemplo Ltda",
            jobTitle: "Gerente de Vendas",
            confidenceScores: new Dictionary<string, decimal>
            {
                { "name", 0.95m },
                { "email", 0.98m },
                { "phone", 0.92m },
                { "company", 0.90m },
                { "jobTitle", 0.88m }
            }
        );

        return Task.FromResult(sampleExtract);
    }
}


