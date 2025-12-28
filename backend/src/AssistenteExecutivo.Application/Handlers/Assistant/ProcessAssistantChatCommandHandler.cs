using AssistenteExecutivo.Application.Commands.Assistant;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.Contacts;
using AssistenteExecutivo.Application.Commands.Contacts;
using AssistenteExecutivo.Application.Queries.Automation;
using AssistenteExecutivo.Application.Commands.Automation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AssistenteExecutivo.Application.Handlers.Assistant;

public class ProcessAssistantChatCommandHandler : IRequestHandler<ProcessAssistantChatCommand, ProcessAssistantChatResult>
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProcessAssistantChatCommandHandler> _logger;
    private readonly IMediator _mediator;
    private readonly string _model;
    private readonly string _apiKey;

    public ProcessAssistantChatCommandHandler(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<ProcessAssistantChatCommandHandler> logger,
        IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
        _httpClient = httpClientFactory.CreateClient();

        var baseUrl = configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1/";
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.Timeout = TimeSpan.FromMinutes(10);

        _apiKey = configuration["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException("OpenAI:ApiKey não configurado");

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        _model = configuration["OpenAI:LLM:Model"] ?? "gpt-4o-mini";
    }

    public async Task<ProcessAssistantChatResult> Handle(ProcessAssistantChatCommand request, CancellationToken cancellationToken)
    {
        //try
        //{
            var model = request.Model ?? _model;
            var tools = GetFunctionDefinitions();

            // Preparar mensagens
            var inputMessages = request.Messages.Select(m => (object)new
            {
                role = m.Role,
                content = m.Content
            }).ToList();

            // Primeira chamada ao OpenAI
            var openaiResponse = await CallOpenAIAsync(model, inputMessages, tools, cancellationToken);

            if (openaiResponse.Choices == null || openaiResponse.Choices.Count == 0)
            {
                return new ProcessAssistantChatResult
                {
                    Message = "Não foi possível gerar uma resposta.",
                    FunctionCalls = new List<FunctionCallInfo>()
                };
            }

            var message = openaiResponse.Choices[0].Message;
            var toolCalls = message?.ToolCalls ?? new List<ToolCall>();

            // Se houver function calls, executá-las
            if (toolCalls.Count > 0)
            {
                var functionCalls = new List<FunctionCallInfo>();
                var functionResults = new List<object>();

                foreach (var toolCall in toolCalls)
                {
                    if (toolCall.Function?.Name != null && toolCall.Function?.Arguments != null)
                    {
                        functionCalls.Add(new FunctionCallInfo
                        {
                            Name = toolCall.Function.Name,
                            Arguments = toolCall.Function.Arguments
                        });

                        // Executar função
                        var result = await ExecuteFunctionAsync(
                            request.OwnerUserId,
                            toolCall.Function.Name,
                            toolCall.Function.Arguments,
                            cancellationToken);

                        functionResults.Add(new
                        {
                            role = "tool",
                            tool_call_id = toolCall.Id,
                            content = JsonSerializer.Serialize(result)
                        });
                    }
                }

                // Adicionar mensagem do assistente com tool calls
                inputMessages.Add((object)new Dictionary<string, object>
                {
                    ["role"] = "assistant",
                    ["content"] = message?.Content ?? (object?)null!,
                    ["tool_calls"] = toolCalls.Select(tc => new Dictionary<string, object>
                    {
                        ["id"] = tc.Id,
                        ["type"] = "function",
                        ["function"] = new Dictionary<string, object?>
                        {
                            ["name"] = tc.Function?.Name,
                            ["arguments"] = tc.Function?.Arguments
                        }
                    }).ToList()
                });

                // Adicionar resultados das funções
                inputMessages.AddRange(functionResults);

                // Segunda chamada ao OpenAI com os resultados
                var finalResponse = await CallOpenAIAsync(model, inputMessages, tools, cancellationToken);

                if (finalResponse.Choices != null && finalResponse.Choices.Count > 0)
                {
                    var finalMessage = finalResponse.Choices[0].Message?.Content ?? "Resposta processada com sucesso.";
                    return new ProcessAssistantChatResult
                    {
                        Message = finalMessage,
                        FunctionCalls = functionCalls
                    };
                }
            }

            // Se não houver function calls, retornar resposta direta
            return new ProcessAssistantChatResult
            {
                Message = message?.Content ?? "Não foi possível gerar uma resposta.",
                FunctionCalls = new List<FunctionCallInfo>()
            };
        //}
        //catch (Exception ex)
        //{
        //    _logger.LogError(ex, "Erro ao processar chat do assistente");
        //    throw;
        //}
    }

    private async Task<OpenAIResponse> CallOpenAIAsync(
        string model,
        List<object> messages,
        List<object> tools,
        CancellationToken cancellationToken)
    {
        var requestBody = new
        {
            model,
            messages,
            tools,
            tool_choice = "auto"
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("chat/completions", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Erro ao chamar OpenAI API: {StatusCode}, {Error}", response.StatusCode, errorContent);
            throw new HttpRequestException($"Erro ao chamar OpenAI API: {response.StatusCode}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        
        // Log para debug
        _logger.LogDebug("OpenAI Response JSON: {Response}", responseJson);
        
        // Configurar opções de deserialização para aceitar camelCase do OpenAI
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        
        var openaiResponse = JsonSerializer.Deserialize<OpenAIResponse>(responseJson, options) ?? new OpenAIResponse();
        
        // Log para debug
        _logger.LogDebug("Deserialized OpenAI Response - Choices count: {Count}", openaiResponse.Choices?.Count ?? 0);
        if (openaiResponse.Choices != null && openaiResponse.Choices.Count > 0)
        {
            _logger.LogDebug("First choice message content: {Content}", openaiResponse.Choices[0].Message?.Content);
            _logger.LogDebug("First choice tool calls count: {Count}", openaiResponse.Choices[0].Message?.ToolCalls?.Count ?? 0);
        }
        
        return openaiResponse;
    }

    private async Task<object> ExecuteFunctionAsync(
        Guid ownerUserId,
        string functionName,
        string argumentsJson,
        CancellationToken cancellationToken)
    {
        try
        {
            var args = JsonSerializer.Deserialize<JsonElement>(argumentsJson);

            return functionName switch
            {
                "list_contacts" => await ExecuteListContactsAsync(ownerUserId, args, cancellationToken),
                "search_contacts" => await ExecuteSearchContactsAsync(ownerUserId, args, cancellationToken),
                "get_contact" => await ExecuteGetContactAsync(ownerUserId, args, cancellationToken),
                "create_contact" => await ExecuteCreateContactAsync(ownerUserId, args, cancellationToken),
                "create_reminder" => await ExecuteCreateReminderAsync(ownerUserId, args, cancellationToken),
                "list_reminders" => await ExecuteListRemindersAsync(ownerUserId, args, cancellationToken),
                "get_reminder" => await ExecuteGetReminderAsync(ownerUserId, args, cancellationToken),
                _ => throw new NotSupportedException($"Função não suportada: {functionName}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao executar função {FunctionName}", functionName);
            return new { error = ex.Message };
        }
    }

    private async Task<object> ExecuteListContactsAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        var page = args.TryGetProperty("page", out var pageProp) ? pageProp.GetInt32() : 1;
        var pageSize = args.TryGetProperty("pageSize", out var pageSizeProp) ? pageSizeProp.GetInt32() : 20;
        var includeDeleted = args.TryGetProperty("includeDeleted", out var deletedProp) && deletedProp.GetBoolean();

        var query = new ListContactsQuery
        {
            OwnerUserId = ownerUserId,
            Page = page,
            PageSize = pageSize,
            IncludeDeleted = includeDeleted
        };

        var result = await _mediator.Send(query, cancellationToken);
        return result;
    }

    private async Task<object> ExecuteSearchContactsAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("searchTerm", out var searchTermProp))
            throw new ArgumentException("searchTerm é obrigatório");

        var searchTerm = searchTermProp.GetString() ?? "";
        var page = args.TryGetProperty("page", out var pageProp) ? pageProp.GetInt32() : 1;
        var pageSize = args.TryGetProperty("pageSize", out var pageSizeProp) ? pageSizeProp.GetInt32() : 20;

        var query = new SearchContactsQuery
        {
            OwnerUserId = ownerUserId,
            SearchTerm = searchTerm,
            Page = page,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query, cancellationToken);
        return result;
    }

    private async Task<object> ExecuteGetContactAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("id", out var idProp))
            throw new ArgumentException("id é obrigatório");

        var contactId = Guid.Parse(idProp.GetString() ?? "");

        var query = new GetContactByIdQuery
        {
            OwnerUserId = ownerUserId,
            ContactId = contactId
        };

        var result = await _mediator.Send(query, cancellationToken);
        if (result == null)
            throw new InvalidOperationException("Contato não encontrado");
        
        return result;
    }

    private async Task<object> ExecuteCreateContactAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("firstName", out var firstNameProp))
            throw new ArgumentException("firstName é obrigatório");

        var command = new CreateContactCommand
        {
            OwnerUserId = ownerUserId,
            FirstName = firstNameProp.GetString() ?? "",
            LastName = args.TryGetProperty("lastName", out var lastNameProp) ? lastNameProp.GetString() : null,
            JobTitle = args.TryGetProperty("jobTitle", out var jobTitleProp) ? jobTitleProp.GetString() : null,
            Company = args.TryGetProperty("company", out var companyProp) ? companyProp.GetString() : null,
            Street = args.TryGetProperty("street", out var streetProp) ? streetProp.GetString() : null,
            City = args.TryGetProperty("city", out var cityProp) ? cityProp.GetString() : null,
            State = args.TryGetProperty("state", out var stateProp) ? stateProp.GetString() : null,
            ZipCode = args.TryGetProperty("zipCode", out var zipCodeProp) ? zipCodeProp.GetString() : null,
            Country = args.TryGetProperty("country", out var countryProp) ? countryProp.GetString() : null
        };

        var contactId = await _mediator.Send(command, cancellationToken);
        return new { contactId };
    }

    private async Task<object> ExecuteCreateReminderAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("contactId", out var contactIdProp) ||
            !args.TryGetProperty("reason", out var reasonProp) ||
            !args.TryGetProperty("scheduledFor", out var scheduledForProp))
            throw new ArgumentException("contactId, reason e scheduledFor são obrigatórios");

        var command = new CreateReminderCommand
        {
            OwnerUserId = ownerUserId,
            ContactId = Guid.Parse(contactIdProp.GetString() ?? ""),
            Reason = reasonProp.GetString() ?? "",
            SuggestedMessage = args.TryGetProperty("suggestedMessage", out var msgProp) ? msgProp.GetString() : null,
            ScheduledFor = DateTime.Parse(scheduledForProp.GetString() ?? "")
        };

        var reminderId = await _mediator.Send(command, cancellationToken);
        return new { reminderId };
    }

    private async Task<object> ExecuteListRemindersAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        Guid? contactId = null;
        if (args.TryGetProperty("contactId", out var contactIdProp) && contactIdProp.ValueKind != JsonValueKind.Null)
            contactId = Guid.Parse(contactIdProp.GetString() ?? "");

        var query = new ListRemindersQuery
        {
            OwnerUserId = ownerUserId,
            ContactId = contactId,
            Page = args.TryGetProperty("page", out var pageProp) ? pageProp.GetInt32() : 1,
            PageSize = args.TryGetProperty("pageSize", out var pageSizeProp) ? pageSizeProp.GetInt32() : 20
        };

        var result = await _mediator.Send(query, cancellationToken);
        return result;
    }

    private async Task<object> ExecuteGetReminderAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("id", out var idProp))
            throw new ArgumentException("id é obrigatório");

        var reminderId = Guid.Parse(idProp.GetString() ?? "");

        var query = new GetReminderByIdQuery
        {
            OwnerUserId = ownerUserId,
            ReminderId = reminderId
        };

        var result = await _mediator.Send(query, cancellationToken);
        if (result == null)
            throw new InvalidOperationException("Lembrete não encontrado");
        
        return result;
    }

    private List<object> GetFunctionDefinitions()
    {
        // Estrutura correta para OpenAI API: cada tool deve ter um objeto "function" dentro
        return new List<object>
        {
            new
            {
                type = "function",
                function = new
                {
                    name = "list_contacts",
                    description = "Lista todos os contatos do usuário autenticado. Suporta paginação e filtros.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            page = new { type = new[] { "number", "null" }, description = "Número da página (padrão: 1)" },
                            pageSize = new { type = new[] { "number", "null" }, description = "Tamanho da página (padrão: 20)" },
                            includeDeleted = new { type = new[] { "boolean", "null" }, description = "Incluir contatos deletados (padrão: false)" }
                        },
                        required = new[] { "page", "pageSize", "includeDeleted" },
                        additionalProperties = false
                    },
                    strict = true
                }
            },
            new
            {
                type = "function",
                function = new
                {
                    name = "search_contacts",
                    description = "Busca contatos com filtro de texto. Útil para encontrar contatos por nome, empresa, email, etc.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            searchTerm = new { type = "string", description = "Termo de busca (nome, empresa, email, etc)" },
                            page = new { type = new[] { "number", "null" }, description = "Número da página (padrão: 1)" },
                            pageSize = new { type = new[] { "number", "null" }, description = "Tamanho da página (padrão: 20)" }
                        },
                        required = new[] { "searchTerm", "page", "pageSize" },
                        additionalProperties = false
                    },
                    strict = true
                }
            },
            new
            {
                type = "function",
                function = new
                {
                    name = "get_contact",
                    description = "Obtém um contato específico por ID",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            id = new { type = "string", description = "ID do contato (GUID)" }
                        },
                        required = new[] { "id" },
                        additionalProperties = false
                    },
                    strict = true
                }
            },
            new
            {
                type = "function",
                function = new
                {
                    name = "create_contact",
                    description = "Cria um novo contato. Primeiro nome é obrigatório, outros campos são opcionais.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            firstName = new { type = "string", description = "Primeiro nome (obrigatório)" },
                            lastName = new { type = new[] { "string", "null" }, description = "Sobrenome" },
                            jobTitle = new { type = new[] { "string", "null" }, description = "Cargo" },
                            company = new { type = new[] { "string", "null" }, description = "Empresa" },
                            street = new { type = new[] { "string", "null" }, description = "Endereço (rua)" },
                            city = new { type = new[] { "string", "null" }, description = "Cidade" },
                            state = new { type = new[] { "string", "null" }, description = "Estado" },
                            zipCode = new { type = new[] { "string", "null" }, description = "CEP" },
                            country = new { type = new[] { "string", "null" }, description = "País" }
                        },
                        required = new[] { "firstName", "lastName", "jobTitle", "company", "street", "city", "state", "zipCode", "country" },
                        additionalProperties = false
                    },
                    strict = true
                }
            },
            new
            {
                type = "function",
                function = new
                {
                    name = "create_reminder",
                    description = "Cria um novo lembrete para um contato. Útil para agendar follow-ups ou tarefas.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            contactId = new { type = "string", description = "ID do contato (GUID)" },
                            reason = new { type = "string", description = "Motivo do lembrete (máximo 500 caracteres)" },
                            suggestedMessage = new { type = new[] { "string", "null" }, description = "Mensagem sugerida para o lembrete (máximo 2000 caracteres)" },
                            scheduledFor = new { type = "string", description = "Data e hora agendada (formato ISO 8601, ex: 2024-12-25T10:00:00Z)" }
                        },
                        required = new[] { "contactId", "reason", "scheduledFor", "suggestedMessage" },
                        additionalProperties = false
                    },
                    strict = true
                }
            },
            new
            {
                type = "function",
                function = new
                {
                    name = "list_reminders",
                    description = "Lista lembretes do usuário autenticado. Suporta filtros por contato, status e data.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            contactId = new { type = new[] { "string", "null" }, description = "Filtrar por ID do contato (GUID)" },
                            status = new { type = new[] { "string", "null" }, description = "Filtrar por status (Pending, Completed, Cancelled)", @enum = new[] { "Pending", "Completed", "Cancelled" } },
                            startDate = new { type = new[] { "string", "null" }, description = "Data inicial (formato ISO 8601)" },
                            endDate = new { type = new[] { "string", "null" }, description = "Data final (formato ISO 8601)" },
                            page = new { type = new[] { "number", "null" }, description = "Número da página (padrão: 1)" },
                            pageSize = new { type = new[] { "number", "null" }, description = "Tamanho da página (padrão: 20)" }
                        },
                        required = new[] { "contactId", "status", "startDate", "endDate", "page", "pageSize" },
                        additionalProperties = false
                    },
                    strict = true
                }
            },
            new
            {
                type = "function",
                function = new
                {
                    name = "get_reminder",
                    description = "Obtém um lembrete específico por ID",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            id = new { type = "string", description = "ID do lembrete (GUID)" }
                        },
                        required = new[] { "id" },
                        additionalProperties = false
                    },
                    strict = true
                }
            }
        };
    }
}

// Classes auxiliares para deserialização da resposta do OpenAI
// Usar JsonPropertyName para mapear corretamente os campos do JSON
public class OpenAIResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("choices")]
    public List<Choice>? Choices { get; set; }
}

public class Choice
{
    [System.Text.Json.Serialization.JsonPropertyName("message")]
    public Message? Message { get; set; }
}

public class Message
{
    [System.Text.Json.Serialization.JsonPropertyName("content")]
    public string? Content { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("tool_calls")]
    public List<ToolCall>? ToolCalls { get; set; }
}

public class ToolCall
{
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [System.Text.Json.Serialization.JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [System.Text.Json.Serialization.JsonPropertyName("function")]
    public FunctionCall? Function { get; set; }
}

public class FunctionCall
{
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("arguments")]
    public string? Arguments { get; set; }
}

