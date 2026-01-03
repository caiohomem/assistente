using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AssistenteExecutivo.Infrastructure.Services.Negotiations;

public class OpenAINegotiationAIService : INegotiationAIService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAINegotiationAIService> _logger;
    private readonly string _model;
    private readonly double _temperature;
    private readonly int _maxTokens;

    public OpenAINegotiationAIService(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<OpenAINegotiationAIService> logger)
    {
        _logger = logger;
        var baseUrl = configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1/";
        var apiKey = configuration["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException("OpenAI:ApiKey não configurado.");

        _httpClient = httpClientFactory.CreateClient();
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var org = configuration["OpenAI:OrganizationId"];
        if (!string.IsNullOrWhiteSpace(org))
            _httpClient.DefaultRequestHeaders.Add("OpenAI-Organization", org);

        _model = configuration["OpenAI:LLM:Model"] ?? "gpt-4o-mini";
        _temperature = double.TryParse(configuration["OpenAI:LLM:Temperature"], out var temp) ? Math.Clamp(temp, 0, 2) : 0.4;
        _maxTokens = int.TryParse(configuration["OpenAI:LLM:MaxTokens"], out var mt) ? mt : 1500;
    }

    public async Task<NegotiationSuggestion> SuggestIntermediateTermsAsync(
        string negotiationContext,
        IReadOnlyCollection<NegotiationProposalSnapshot> proposals,
        CancellationToken cancellationToken = default)
    {
        var prompt = BuildSuggestionPrompt(negotiationContext, proposals);
        var response = await SendChatCompletionAsync(prompt, cancellationToken);

        try
        {
            var document = JsonDocument.Parse(response);
            var root = document.RootElement;
            var summary = root.TryGetProperty("summary", out var summaryEl)
                ? summaryEl.GetString() ?? "Proposta gerada."
                : "Proposta gerada.";

            var termsJson = root.TryGetProperty("terms", out var termsEl)
                ? termsEl.GetRawText()
                : root.GetRawText();

            return new NegotiationSuggestion
            {
                Summary = summary,
                SuggestedTermsJson = termsJson
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Falha ao interpretar resposta de sugestão de negociação. Conteúdo: {Response}", response);
            return new NegotiationSuggestion
            {
                Summary = "Sugestão AI indisponível.",
                SuggestedTermsJson = JsonSerializer.Serialize(new
                {
                    error = "Falha ao interpretar resposta da IA",
                    raw = response
                })
            };
        }
    }

    public async Task<string> GenerateAgreementDraftAsync(
        string negotiationContext,
        IReadOnlyCollection<NegotiationProposalSnapshot> proposals,
        CancellationToken cancellationToken = default)
    {
        var prompt = BuildAgreementPrompt(negotiationContext, proposals);
        return await SendChatCompletionAsync(prompt, cancellationToken);
    }

    private async Task<string> SendChatCompletionAsync(string prompt, CancellationToken cancellationToken)
    {
        var body = new
        {
            model = _model,
            messages = new[]
            {
                new { role = "system", content = "Você é um assistente especializado em negociações comerciais." },
                new { role = "user", content = prompt }
            },
            temperature = _temperature,
            max_tokens = _maxTokens,
            response_format = new { type = "json_object" }
        };

        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("chat/completions", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Falha ao chamar OpenAI. Status: {Status}, Detalhes: {Error}", response.StatusCode, error);
            throw new HttpRequestException($"OpenAI retornou erro {response.StatusCode}: {error}");
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var contentValue = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return contentValue ?? string.Empty;
    }

    private static string BuildSuggestionPrompt(
        string context,
        IReadOnlyCollection<NegotiationProposalSnapshot> proposals)
    {
        var payload = new
        {
            context,
            proposals = proposals.Select(p => new
            {
                proposalId = p.ProposalId,
                partyId = p.PartyId,
                source = p.Source.ToString(),
                status = p.Status.ToString(),
                content = p.Content
            })
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        return $@"Analise a negociação abaixo e produza uma resposta JSON com:
{{
  ""summary"": texto curto,
  ""terms"": {{
      ""payment"": ...,
      ""deadlines"": ...,
      ""conditions"": ...
  }}
}}

Negociação:
{json}";
    }

    private static string BuildAgreementPrompt(
        string context,
        IReadOnlyCollection<NegotiationProposalSnapshot> proposals)
    {
        var accepted = proposals.Where(p => p.Status == ProposalStatus.Accepted).ToList();
        var latest = proposals.OrderByDescending(p => p.ProposalId).Take(3).ToList();

        var payload = new
        {
            context,
            accepted,
            latest
        };

        return $@"Gere um rascunho de acordo completo (em Markdown) com base no contexto e nas propostas:
{JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true })}

Inclua seções de objeto, participantes, valores/marcos e cláusulas principais.
Retorne Markdown válido.";
    }
}
