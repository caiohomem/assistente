using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;
using AssistenteExecutivo.Infrastructure.HttpClients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AssistenteExecutivo.Infrastructure.Services;

/// <summary>
/// Provider LLM usando Ollama para organizar transcrições e gerar sugestões
/// </summary>
public class OllamaLLMProvider : ILLMProvider
{
    private readonly OllamaClient _ollamaClient;
    private readonly ILogger<OllamaLLMProvider> _logger;
    private readonly string _model;
    private readonly double _temperature;
    private readonly int _maxTokens;

    public OllamaLLMProvider(
        OllamaClient ollamaClient,
        IConfiguration configuration,
        ILogger<OllamaLLMProvider> logger)
    {
        _ollamaClient = ollamaClient;
        _logger = logger;
        _model = configuration["Ollama:LLM:Model"] ?? "qwen2.5:7b";
        _temperature = double.Parse(configuration["Ollama:LLM:Temperature"] ?? "0.3");
        _maxTokens = int.Parse(configuration["Ollama:LLM:MaxTokens"] ?? "2000");
    }

    public async Task<AudioProcessingResult> SummarizeAndExtractTasksAsync(
        string transcript,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(transcript))
            {
                _logger.LogWarning("Transcrição vazia recebida");
                return new AudioProcessingResult
                {
                    Summary = "Transcrição vazia.",
                    Tasks = new List<ExtractedTask>()
                };
            }

            _logger.LogInformation("Processando transcrição com {Length} caracteres usando modelo {Model}", 
                transcript.Length, _model);

            var prompt = BuildPrompt(transcript);

            var response = await _ollamaClient.ChatAsync(
                model: _model,
                message: prompt,
                temperature: _temperature,
                maxTokens: _maxTokens,
                cancellationToken: cancellationToken);

            _logger.LogDebug("Resposta do LLM: {Response}", response);

            // Extrair JSON da resposta
            var jsonText = ExtractJsonFromResponse(response);
            
            if (string.IsNullOrWhiteSpace(jsonText))
            {
                _logger.LogWarning("Não foi possível extrair JSON da resposta do LLM");
                // Fallback: criar resumo simples
                return CreateFallbackResult(transcript);
            }

            // Parse do JSON
            var result = ParseAudioProcessingResult(jsonText, transcript);
            
            _logger.LogInformation(
                "Processamento concluído: Summary length={SummaryLength}, Tasks count={TasksCount}",
                result.Summary.Length, result.Tasks.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar transcrição com LLM");
            // Fallback em caso de erro
            return CreateFallbackResult(transcript);
        }
    }

    private string BuildPrompt(string transcript)
    {
        return $@"Analise a seguinte transcrição de uma nota de áudio sobre um contato e organize as informações de forma estruturada.

TRANSCRIÇÃO:
{transcript}

Extraia e organize as informações em formato JSON válido com a seguinte estrutura:
{{
  ""summary"": ""resumo conciso em 2-3 frases do conteúdo principal"",
  ""categories"": {{
    ""contexto"": ""onde/como conheceu a pessoa (evento, local, situação)"",
    ""profissao"": ""cargo, empresa, área de atuação mencionada"",
    ""interesses"": ""interesses pessoais, hobbies, preferências mencionadas"",
    ""datas"": ""datas importantes mencionadas (férias, eventos, prazos, etc)"",
    ""oportunidades"": ""oportunidades de negócio, parcerias, projetos mencionados""
  }},
  ""suggestions"": [
    ""sugestão 1 de ação concreta para próxima interação"",
    ""sugestão 2 de ação concreta"",
    ""sugestão 3 de ação concreta""
  ]
}}

IMPORTANTE:
- Retorne APENAS o JSON válido, sem markdown, sem texto adicional, sem explicações
- Se alguma categoria não tiver informação relevante, use string vazia ou null
- As sugestões devem ser ações práticas e específicas baseadas no conteúdo
- O resumo deve capturar os pontos principais de forma concisa
- Use português brasileiro em todas as respostas";
    }

    private string ExtractJsonFromResponse(string response)
    {
        // Tentar encontrar JSON na resposta
        var jsonMatch = System.Text.RegularExpressions.Regex.Match(
            response,
            @"\{[^{}]*""summary""[^{}]*(?:\{[^{}]*\}[^{}]*)*\}[^{}]*""suggestions""[^{}]*\[[^\]]*\]\s*\}",
            System.Text.RegularExpressions.RegexOptions.Singleline);

        if (jsonMatch.Success)
        {
            return jsonMatch.Value;
        }

        // Tentar encontrar entre markdown code blocks
        var markdownMatch = System.Text.RegularExpressions.Regex.Match(
            response,
            @"```(?:json)?\s*(\{.*?\})\s*```",
            System.Text.RegularExpressions.RegexOptions.Singleline);

        if (markdownMatch.Success)
        {
            return markdownMatch.Groups[1].Value;
        }

        // Tentar encontrar qualquer JSON válido que contenha "summary"
        var anyJsonMatch = System.Text.RegularExpressions.Regex.Match(
            response,
            @"\{[^{}]*""summary""[^{}]*(?:\{[^{}]*\}[^{}]*)*\}",
            System.Text.RegularExpressions.RegexOptions.Singleline);

        if (anyJsonMatch.Success)
        {
            return anyJsonMatch.Value;
        }

        return response.Trim();
    }

    private AudioProcessingResult ParseAudioProcessingResult(string jsonText, string transcript)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonText);
            var root = doc.RootElement;

            var summary = root.TryGetProperty("summary", out var summaryProp)
                ? summaryProp.GetString()?.Trim() ?? CreateSimpleSummary(transcript)
                : CreateSimpleSummary(transcript);

            var tasks = new List<ExtractedTask>();

            // Extrair sugestões como tasks
            if (root.TryGetProperty("suggestions", out var suggestionsProp) &&
                suggestionsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var suggestion in suggestionsProp.EnumerateArray())
                {
                    var suggestionText = suggestion.GetString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(suggestionText))
                    {
                        // Tentar extrair data se mencionada na sugestão
                        DateTime? dueDate = ExtractDateFromText(suggestionText);
                        
                        tasks.Add(new ExtractedTask(
                            description: suggestionText,
                            dueDate: dueDate,
                            priority: dueDate.HasValue ? "medium" : "low"));
                    }
                }
            }

            // Se não houver sugestões, criar uma genérica
            if (tasks.Count == 0)
            {
                tasks.Add(new ExtractedTask(
                    description: "Revisar informações do contato e planejar próxima interação",
                    dueDate: null,
                    priority: "low"));
            }

            return new AudioProcessingResult
            {
                Summary = summary,
                Tasks = tasks
            };
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Erro ao fazer parse do JSON: {JsonText}", jsonText);
            return CreateFallbackResult(transcript);
        }
    }

    private AudioProcessingResult CreateFallbackResult(string transcript)
    {
        var summary = CreateSimpleSummary(transcript);
        var tasks = new List<ExtractedTask>
        {
            new ExtractedTask(
                description: "Revisar informações do contato e planejar próxima interação",
                dueDate: null,
                priority: "low")
        };

        return new AudioProcessingResult
        {
            Summary = summary,
            Tasks = tasks
        };
    }

    private string CreateSimpleSummary(string transcript)
    {
        if (string.IsNullOrWhiteSpace(transcript))
        {
            return "Nota de áudio processada.";
        }

        // Criar resumo simples: primeiras 200 caracteres + "..."
        if (transcript.Length <= 200)
        {
            return transcript;
        }

        return transcript.Substring(0, 200) + "...";
    }

    private DateTime? ExtractDateFromText(string text)
    {
        // Regex simples para detectar datas mencionadas
        // Exemplos: "antes de julho", "em julho", "até dia 15", etc.
        var datePatterns = new[]
        {
            @"(antes de|até|até dia)\s+(\d{1,2})",
            @"(em|durante)\s+(janeiro|fevereiro|março|abril|maio|junho|julho|agosto|setembro|outubro|novembro|dezembro)",
        };

        // Por simplicidade, retornar null
        // Em produção, poderia usar biblioteca de NLP para extrair datas
        return null;
    }
}

