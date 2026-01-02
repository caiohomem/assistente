using AssistenteExecutivo.Application.Commands.Assistant;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.Contacts;
using AssistenteExecutivo.Application.Commands.Contacts;
using AssistenteExecutivo.Application.Queries.Automation;
using AssistenteExecutivo.Application.Commands.Automation;
using AssistenteExecutivo.Application.Queries.Notes;
using AssistenteExecutivo.Application.Commands.Notes;
using AssistenteExecutivo.Application.Queries.AgentConfiguration;
using AssistenteExecutivo.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Globalization;

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
            ?? throw new InvalidOperationException("OpenAI:ApiKey nÃ£o configurado");

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        _model = configuration["OpenAI:LLM:Model"] ?? "gpt-4o-mini";
    }

    public async Task<ProcessAssistantChatResult> Handle(ProcessAssistantChatCommand request, CancellationToken cancellationToken)
    {
        var model = request.Model ?? _model;
        var workflowPromptContext = await GetWorkflowPromptAsync(cancellationToken);
        var tools = GetFunctionDefinitions(workflowPromptContext);

        // Preparar input items (Responses API usa input em vez de messages)
        var inputItems = request.Messages.Select(m => (object)new InputMessage
        {
            Role = m.Role,
            Content = m.Content
        }).ToList();

        // Primeira chamada ao OpenAI
        var openaiResponse = await CallOpenAIAsync(model, inputItems, tools, cancellationToken);

        if (openaiResponse.Output == null || openaiResponse.Output.Count == 0)
        {
            return new ProcessAssistantChatResult
            {
                Message = "NÃ£o foi possÃ­vel gerar uma resposta.",
                FunctionCalls = new List<FunctionCallInfo>()
            };
        }

        var functionCalls = new List<FunctionCallInfo>();
        var hasFunctionCalls = false;
        var finalTextContent = "";

        // Processar output items
        foreach (var outputItem in openaiResponse.Output)
        {
            // Verificar se Ã© function_call (campos diretos na Responses API)
            if (outputItem.Type == "function_call" && !string.IsNullOrEmpty(outputItem.Name))
            {
                hasFunctionCalls = true;

                functionCalls.Add(new FunctionCallInfo
                {
                    Name = outputItem.Name,
                    Arguments = outputItem.Arguments ?? ""
                });

                // Executar funÃ§Ã£o
                var result = await ExecuteFunctionAsync(
                    request.OwnerUserId,
                    outputItem.Name,
                    outputItem.Arguments ?? "",
                    cancellationToken);

                // Adicionar o function_call original ao input (Responses API requer isso)
                inputItems.Add(new InputFunctionCall
                {
                    CallId = outputItem.CallId ?? "",
                    Name = outputItem.Name,
                    Arguments = outputItem.Arguments ?? ""
                });

                // Adicionar resultado ao input para prÃ³xima chamada
                inputItems.Add(new InputFunctionCallOutput
                {
                    CallId = outputItem.CallId ?? "",
                    Output = JsonSerializer.Serialize(result)
                });
            }
            // Verificar se Ã© text direto
            else if (outputItem.Type == "text" && outputItem.Text != null)
            {
                finalTextContent = outputItem.Text;
            }
            // Verificar se Ã© message (Responses API retorna assim)
            else if (outputItem.Type == "message" && outputItem.Content != null)
            {
                var textPart = outputItem.Content.FirstOrDefault(c => c.Type == "output_text" && !string.IsNullOrEmpty(c.Text));
                if (textPart != null)
                {
                    finalTextContent = textPart.Text!;
                }
            }
        }

        // Se houver function calls, continuar chamando atÃ© resolver (limite de seguranÃ§a de 3 iteraÃ§Ãµes)
        var safetyIterations = 0;
        while (hasFunctionCalls && safetyIterations < 5)
        {
            hasFunctionCalls = false;
            var followUpResponse = await CallOpenAIAsync(model, inputItems, tools, cancellationToken);

            if (followUpResponse.Output == null || followUpResponse.Output.Count == 0)
            {
                break;
            }

            foreach (var outputItem in followUpResponse.Output)
            {
                if (outputItem.Type == "function_call" && !string.IsNullOrEmpty(outputItem.Name))
                {
                    hasFunctionCalls = true;

                    functionCalls.Add(new FunctionCallInfo
                    {
                        Name = outputItem.Name,
                        Arguments = outputItem.Arguments ?? ""
                    });

                    var result = await ExecuteFunctionAsync(
                        request.OwnerUserId,
                        outputItem.Name,
                        outputItem.Arguments ?? "",
                        cancellationToken);

                    inputItems.Add(new InputFunctionCall
                    {
                        CallId = outputItem.CallId ?? "",
                        Name = outputItem.Name,
                        Arguments = outputItem.Arguments ?? ""
                    });

                    inputItems.Add(new InputFunctionCallOutput
                    {
                        CallId = outputItem.CallId ?? "",
                        Output = JsonSerializer.Serialize(result)
                    });
                }
                else if (outputItem.Type == "text" && outputItem.Text != null)
                {
                    finalTextContent = outputItem.Text;
                }
                else if (outputItem.Type == "message" && outputItem.Content != null)
                {
                    var textPart = outputItem.Content.FirstOrDefault(c => c.Type == "output_text" && !string.IsNullOrEmpty(c.Text));
                    if (textPart != null)
                    {
                        finalTextContent = textPart.Text!;
                    }
                }
            }

            safetyIterations++;
            if (!hasFunctionCalls)
            {
                break;
            }
        }

        if (finalTextContent != "")
        {
            return new ProcessAssistantChatResult
            {
                Message = finalTextContent,
                FunctionCalls = functionCalls
            };
        }

        // Se nÃ£o houver function calls, retornar resposta direta
        return new ProcessAssistantChatResult
        {
            Message = finalTextContent != "" ? finalTextContent : "NÃ£o foi possÃ­vel gerar uma resposta.",
            FunctionCalls = functionCalls
        };
    }

    private async Task<OpenAIResponse> CallOpenAIAsync(
        string model,
        List<object> inputItems,
        List<object> tools,
        CancellationToken cancellationToken)
    {
        // Configurar opÃ§Ãµes de serializaÃ§Ã£o para garantir formato correto
        // NÃ£o usar PropertyNamingPolicy para manter snake_case (tool_choice) e camelCase conforme necessÃ¡rio
        var serializeOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        // Serializar tools primeiro para verificar estrutura
        var toolsJson = JsonSerializer.Serialize(tools, serializeOptions);
        _logger.LogDebug("Tools JSON (first 500 chars): {ToolsJson}",
            toolsJson.Length > 500 ? toolsJson.Substring(0, 500) + "..." : toolsJson);

        // Deserializar tools para garantir que a estrutura estÃ¡ correta
        using var toolsDoc = JsonDocument.Parse(toolsJson);
        var toolsArray = toolsDoc.RootElement.Clone();

        // Construir request body usando objeto anÃ´nimo - JsonSerializer lida melhor com isso
        var requestBody = new
        {
            model = model,
            input = inputItems,
            tools = tools, // Usar tools original, nÃ£o o serializado
            tool_choice = "auto"
        };

        var json = JsonSerializer.Serialize(requestBody, serializeOptions);

        // Log detalhado do JSON para debug
        _logger.LogDebug("Request JSON completo (length: {Length}): {Json}", json.Length, json);

        // Verificar estrutura dos tools no JSON gerado
        try
        {
            using var jsonDoc = JsonDocument.Parse(json);
            if (jsonDoc.RootElement.TryGetProperty("tools", out var toolsElement))
            {
                if (toolsElement.ValueKind == JsonValueKind.Array && toolsElement.GetArrayLength() > 0)
                {
                    var firstTool = toolsElement[0];
                    _logger.LogDebug("Primeiro tool no JSON: {ToolJson}", firstTool.GetRawText());

                    if (firstTool.TryGetProperty("type", out var typeProp) &&
                        firstTool.TryGetProperty("name", out var nameProp))
                    {
                        _logger.LogDebug("Tool tem estrutura correta: type={Type}, name={Name}",
                            typeProp.GetString(), nameProp.GetString());
                    }
                    else
                    {
                        _logger.LogError("Tool NAO tem 'type' ou 'name' no nA-vel raiz! Estrutura: {ToolJson}", firstTool.GetRawText());
                    }
                }
                else
                {
                    _logger.LogWarning("Tools array estA? vazio ou invA?lido");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao analisar JSON gerado");
        }

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("responses", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Erro ao chamar OpenAI API: {StatusCode}, {Error}. Payload enviado: {Payload}", response.StatusCode, errorContent, json);
            throw new HttpRequestException($"Erro ao chamar OpenAI API: {response.StatusCode}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        // Log para debug
        _logger.LogDebug("OpenAI Response JSON: {Response}", responseJson);

        // Configurar opÃ§Ãµes de deserializaÃ§Ã£o para aceitar camelCase do OpenAI
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var openaiResponse = JsonSerializer.Deserialize<OpenAIResponse>(responseJson, options) ?? new OpenAIResponse();

        // Log para debug
        _logger.LogDebug("Deserialized OpenAI Response - Output count: {Count}", openaiResponse.Output?.Count ?? 0);
        if (openaiResponse.Output != null && openaiResponse.Output.Count > 0)
        {
            foreach (var outputItem in openaiResponse.Output)
            {
                _logger.LogDebug("Output item type: {Type}", outputItem.Type);
                if (outputItem.Type == "text")
                {
                    _logger.LogDebug("Text content: {Content}", outputItem.Text);
                }
                else if (outputItem.Type == "function_call")
                {
                    _logger.LogDebug("Function call: {Name}", outputItem.Name);
                }
            }
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
                // Contatos
                "list_contacts" => await ExecuteListContactsAsync(ownerUserId, args, cancellationToken),
                "search_contacts" => await ExecuteSearchContactsAsync(ownerUserId, args, cancellationToken),
                "get_contact" => await ExecuteGetContactAsync(ownerUserId, args, cancellationToken),
                "create_contact" => await ExecuteCreateContactAsync(ownerUserId, args, cancellationToken),
                "update_contact" => await ExecuteUpdateContactAsync(ownerUserId, args, cancellationToken),
                "delete_contact" => await ExecuteDeleteContactAsync(ownerUserId, args, cancellationToken),
                "add_contact_email" => await ExecuteAddContactEmailAsync(ownerUserId, args, cancellationToken),
                "add_contact_phone" => await ExecuteAddContactPhoneAsync(ownerUserId, args, cancellationToken),
                "add_contact_tag" => await ExecuteAddContactTagAsync(ownerUserId, args, cancellationToken),
                "add_contact_relationship" => await ExecuteAddContactRelationshipAsync(ownerUserId, args, cancellationToken),
                "delete_contact_relationship" => await ExecuteDeleteContactRelationshipAsync(ownerUserId, args, cancellationToken),
                "get_network_graph" => await ExecuteGetNetworkGraphAsync(ownerUserId, args, cancellationToken),
                // Lembretes
                "create_reminder" => await ExecuteCreateReminderAsync(ownerUserId, args, cancellationToken),
                "list_reminders" => await ExecuteListRemindersAsync(ownerUserId, args, cancellationToken),
                "get_reminder" => await ExecuteGetReminderAsync(ownerUserId, args, cancellationToken),
                "update_reminder_status" => await ExecuteUpdateReminderStatusAsync(ownerUserId, args, cancellationToken),
                "delete_reminder" => await ExecuteDeleteReminderAsync(ownerUserId, args, cancellationToken),
                // Notas
                "list_contact_notes" => await ExecuteListContactNotesAsync(ownerUserId, args, cancellationToken),
                "get_note" => await ExecuteGetNoteAsync(ownerUserId, args, cancellationToken),
                "create_text_note" => await ExecuteCreateTextNoteAsync(ownerUserId, args, cancellationToken),
                "update_note" => await ExecuteUpdateNoteAsync(ownerUserId, args, cancellationToken),
                "delete_note" => await ExecuteDeleteNoteAsync(ownerUserId, args, cancellationToken),
                // Drafts
                "create_draft" => await ExecuteCreateDraftAsync(ownerUserId, args, cancellationToken),
                "list_drafts" => await ExecuteListDraftsAsync(ownerUserId, args, cancellationToken),
                "get_draft" => await ExecuteGetDraftAsync(ownerUserId, args, cancellationToken),
                "update_draft" => await ExecuteUpdateDraftAsync(ownerUserId, args, cancellationToken),
                "approve_draft" => await ExecuteApproveDraftAsync(ownerUserId, args, cancellationToken),
                "send_draft" => await ExecuteSendDraftAsync(ownerUserId, args, cancellationToken),
                "delete_draft" => await ExecuteDeleteDraftAsync(ownerUserId, args, cancellationToken),
                // Templates
                "create_template" => await ExecuteCreateTemplateAsync(ownerUserId, args, cancellationToken),
                "list_templates" => await ExecuteListTemplatesAsync(ownerUserId, args, cancellationToken),
                "get_template" => await ExecuteGetTemplateAsync(ownerUserId, args, cancellationToken),
                "update_template" => await ExecuteUpdateTemplateAsync(ownerUserId, args, cancellationToken),
                "delete_template" => await ExecuteDeleteTemplateAsync(ownerUserId, args, cancellationToken),
                // Letterheads
                "create_letterhead" => await ExecuteCreateLetterheadAsync(ownerUserId, args, cancellationToken),
                "list_letterheads" => await ExecuteListLetterheadsAsync(ownerUserId, args, cancellationToken),
                "get_letterhead" => await ExecuteGetLetterheadAsync(ownerUserId, args, cancellationToken),
                "update_letterhead" => await ExecuteUpdateLetterheadAsync(ownerUserId, args, cancellationToken),
                "delete_letterhead" => await ExecuteDeleteLetterheadAsync(ownerUserId, args, cancellationToken),
                // Workflows
                "create_workflow" => await ExecuteCreateWorkflowAsync(ownerUserId, args, cancellationToken),
                "list_workflows" => await ExecuteListWorkflowsAsync(ownerUserId, args, cancellationToken),
                "get_workflow" => await ExecuteGetWorkflowAsync(ownerUserId, args, cancellationToken),
                "run_workflow" => await ExecuteRunWorkflowAsync(ownerUserId, args, cancellationToken),
                "approve_workflow_step" => await ExecuteApproveWorkflowStepAsync(ownerUserId, args, cancellationToken),
                "list_workflow_executions" => await ExecuteListWorkflowExecutionsAsync(ownerUserId, args, cancellationToken),
                "get_pending_approvals" => await ExecuteGetPendingApprovalsAsync(ownerUserId, cancellationToken),
                _ => throw new NotSupportedException($"FunÃ§Ã£o nÃ£o suportada: {functionName}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao executar funÃ§Ã£o {FunctionName}. Payload: {Payload}", functionName, argumentsJson);
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
            throw new ArgumentException("searchTerm Ã© obrigatÃ³rio");

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
            throw new ArgumentException("id Ã© obrigatÃ³rio");

        var contactId = Guid.Parse(idProp.GetString() ?? "");

        var query = new GetContactByIdQuery
        {
            OwnerUserId = ownerUserId,
            ContactId = contactId
        };

        var result = await _mediator.Send(query, cancellationToken);
        if (result == null)
            throw new InvalidOperationException("Contato nÃ£o encontrado");

        return result;
    }

    private async Task<object> ExecuteCreateContactAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("firstName", out var firstNameProp))
            throw new ArgumentException("firstName Ã© obrigatÃ³rio");

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
            throw new ArgumentException("contactId, reason e scheduledFor sÃ£o obrigatÃ³rios");

        // suggestedMessage Ã© opcional, nÃ£o precisa validar aqui

        var contactIdString = contactIdProp.GetString();
        if (string.IsNullOrWhiteSpace(contactIdString))
            throw new ArgumentException("contactId nÃ£o pode ser vazio");

        if (!Guid.TryParse(contactIdString, out var contactId))
            throw new ArgumentException($"contactId invÃ¡lido: '{contactIdString}'. Deve ser um GUID vÃ¡lido. Use a funÃ§Ã£o search_contacts para encontrar o contato por nome primeiro.");

        var reason = reasonProp.GetString();
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("reason nÃ£o pode ser vazio");

        var scheduledForString = scheduledForProp.GetString();
        if (string.IsNullOrWhiteSpace(scheduledForString))
            throw new ArgumentException("scheduledFor nÃ£o pode ser vazio");

        if (!DateTimeOffset.TryParse(scheduledForString, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var scheduledForDto))
            throw new ArgumentException($"scheduledFor invÃ¡lido: '{scheduledForString}'. Deve estar no formato ISO 8601 (ex: 2024-12-25T10:00:00Z)");
        var scheduledFor = scheduledForDto.UtcDateTime;

        var command = new CreateReminderCommand
        {
            OwnerUserId = ownerUserId,
            ContactId = contactId,
            Reason = reason,
            SuggestedMessage = args.TryGetProperty("suggestedMessage", out var msgProp) && msgProp.ValueKind != JsonValueKind.Null
                ? msgProp.GetString()
                : null,
            ScheduledFor = scheduledFor
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
            throw new ArgumentException("id Ã© obrigatÃ³rio");

        var reminderId = Guid.Parse(idProp.GetString() ?? "");

        var query = new GetReminderByIdQuery
        {
            OwnerUserId = ownerUserId,
            ReminderId = reminderId
        };

        var result = await _mediator.Send(query, cancellationToken);
        if (result == null)
            throw new InvalidOperationException("Lembrete nÃ£o encontrado");

        return result;
    }

    // ========== FunÃ§Ãµes de Contatos Adicionais ==========

    private async Task<object> ExecuteUpdateContactAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("id", out var idProp))
            throw new ArgumentException("id Ã© obrigatÃ³rio");

        var command = new UpdateContactCommand
        {
            ContactId = Guid.Parse(idProp.GetString() ?? ""),
            OwnerUserId = ownerUserId,
            FirstName = args.TryGetProperty("firstName", out var fnProp) ? fnProp.GetString() : null,
            LastName = args.TryGetProperty("lastName", out var lnProp) ? lnProp.GetString() : null,
            JobTitle = args.TryGetProperty("jobTitle", out var jtProp) ? jtProp.GetString() : null,
            Company = args.TryGetProperty("company", out var compProp) ? compProp.GetString() : null,
            Street = args.TryGetProperty("street", out var stProp) ? stProp.GetString() : null,
            City = args.TryGetProperty("city", out var cityProp) ? cityProp.GetString() : null,
            State = args.TryGetProperty("state", out var stateProp) ? stateProp.GetString() : null,
            ZipCode = args.TryGetProperty("zipCode", out var zipProp) ? zipProp.GetString() : null,
            Country = args.TryGetProperty("country", out var countryProp) ? countryProp.GetString() : null
        };

        await _mediator.Send(command, cancellationToken);
        return new { success = true };
    }

    private async Task<object> ExecuteDeleteContactAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("id", out var idProp))
            throw new ArgumentException("id Ã© obrigatÃ³rio");

        var command = new DeleteContactCommand
        {
            ContactId = Guid.Parse(idProp.GetString() ?? ""),
            OwnerUserId = ownerUserId
        };

        await _mediator.Send(command, cancellationToken);
        return new { success = true };
    }

    private async Task<object> ExecuteAddContactEmailAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("contactId", out var contactIdProp) || !args.TryGetProperty("email", out var emailProp))
            throw new ArgumentException("contactId e email sÃ£o obrigatÃ³rios");

        var command = new AddContactEmailCommand
        {
            ContactId = Guid.Parse(contactIdProp.GetString() ?? ""),
            OwnerUserId = ownerUserId,
            Email = emailProp.GetString() ?? ""
        };

        await _mediator.Send(command, cancellationToken);
        return new { success = true };
    }

    private async Task<object> ExecuteAddContactPhoneAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("contactId", out var contactIdProp) || !args.TryGetProperty("phone", out var phoneProp))
            throw new ArgumentException("contactId e phone sÃ£o obrigatÃ³rios");

        var command = new AddContactPhoneCommand
        {
            ContactId = Guid.Parse(contactIdProp.GetString() ?? ""),
            OwnerUserId = ownerUserId,
            Phone = phoneProp.GetString() ?? ""
        };

        await _mediator.Send(command, cancellationToken);
        return new { success = true };
    }

    private async Task<object> ExecuteAddContactTagAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("contactId", out var contactIdProp) || !args.TryGetProperty("tag", out var tagProp))
            throw new ArgumentException("contactId e tag sÃ£o obrigatÃ³rios");

        var command = new AddContactTagCommand
        {
            ContactId = Guid.Parse(contactIdProp.GetString() ?? ""),
            OwnerUserId = ownerUserId,
            Tag = tagProp.GetString() ?? ""
        };

        await _mediator.Send(command, cancellationToken);
        return new { success = true };
    }

    private async Task<object> ExecuteAddContactRelationshipAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("contactId", out var contactIdProp) || !args.TryGetProperty("targetContactId", out var targetProp) || !args.TryGetProperty("type", out var typeProp))
            throw new ArgumentException("contactId, targetContactId e type sÃ£o obrigatÃ³rios");

        var command = new AddContactRelationshipCommand
        {
            ContactId = Guid.Parse(contactIdProp.GetString() ?? ""),
            OwnerUserId = ownerUserId,
            TargetContactId = Guid.Parse(targetProp.GetString() ?? ""),
            Type = typeProp.GetString() ?? "",
            Description = args.TryGetProperty("description", out var descProp) ? descProp.GetString() : null,
            Strength = args.TryGetProperty("strength", out var strengthProp) ? strengthProp.GetSingle() : null,
            IsConfirmed = args.TryGetProperty("isConfirmed", out var confirmedProp) && confirmedProp.GetBoolean()
        };

        await _mediator.Send(command, cancellationToken);
        return new { success = true };
    }

    private async Task<object> ExecuteDeleteContactRelationshipAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("relationshipId", out var relIdProp))
            throw new ArgumentException("relationshipId Ã© obrigatÃ³rio");

        var command = new DeleteRelationshipCommand
        {
            RelationshipId = Guid.Parse(relIdProp.GetString() ?? ""),
            OwnerUserId = ownerUserId
        };

        await _mediator.Send(command, cancellationToken);
        return new { success = true };
    }

    private async Task<object> ExecuteGetNetworkGraphAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        var maxDepth = args.TryGetProperty("maxDepth", out var depthProp) ? depthProp.GetInt32() : 2;

        var query = new GetNetworkGraphQuery
        {
            OwnerUserId = ownerUserId,
            MaxDepth = maxDepth
        };

        var result = await _mediator.Send(query, cancellationToken);
        return result;
    }

    // ========== FunÃ§Ãµes de Lembretes Adicionais ==========

    private async Task<object> ExecuteUpdateReminderStatusAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("id", out var idProp) || !args.TryGetProperty("newStatus", out var statusProp))
            throw new ArgumentException("id e newStatus sÃ£o obrigatÃ³rios");

        if (!Enum.TryParse<ReminderStatus>(statusProp.GetString(), out var status))
            throw new ArgumentException("newStatus deve ser Pending, Sent, Dismissed ou Snoozed");

        var command = new UpdateReminderStatusCommand
        {
            ReminderId = Guid.Parse(idProp.GetString() ?? ""),
            OwnerUserId = ownerUserId,
            NewStatus = status,
            NewScheduledFor = args.TryGetProperty("newScheduledFor", out var dateProp) && dateProp.ValueKind != JsonValueKind.Null
                ? ParseOptionalUtc(dateProp.GetString())
                : null
        };

        await _mediator.Send(command, cancellationToken);
        return new { success = true };
    }

    private async Task<object> ExecuteDeleteReminderAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("id", out var idProp))
            throw new ArgumentException("id Ã© obrigatÃ³rio");

        var command = new DeleteReminderCommand
        {
            ReminderId = Guid.Parse(idProp.GetString() ?? ""),
            OwnerUserId = ownerUserId
        };

        await _mediator.Send(command, cancellationToken);
        return new { success = true };
    }

    // ========== FunÃ§Ãµes de Notas ==========

    private async Task<object> ExecuteListContactNotesAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("contactId", out var contactIdProp))
            throw new ArgumentException("contactId Ã© obrigatÃ³rio");

        var query = new ListNotesByContactQuery
        {
            ContactId = Guid.Parse(contactIdProp.GetString() ?? ""),
            OwnerUserId = ownerUserId
        };

        var result = await _mediator.Send(query, cancellationToken);
        return result;
    }

    private async Task<object> ExecuteGetNoteAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("id", out var idProp))
            throw new ArgumentException("id Ã© obrigatÃ³rio");

        var query = new GetNoteByIdQuery
        {
            NoteId = Guid.Parse(idProp.GetString() ?? ""),
            OwnerUserId = ownerUserId
        };

        var result = await _mediator.Send(query, cancellationToken);
        if (result == null)
            throw new InvalidOperationException("Nota nÃ£o encontrada");

        return result;
    }

    private async Task<object> ExecuteCreateTextNoteAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("contactId", out var contactIdProp) || !args.TryGetProperty("text", out var textProp))
            throw new ArgumentException("contactId e text sÃ£o obrigatÃ³rios");

        var contactIdString = contactIdProp.GetString();
        if (string.IsNullOrWhiteSpace(contactIdString))
            throw new ArgumentException("contactId nÃ£o pode ser vazio");

        if (!Guid.TryParse(contactIdString, out var contactId))
            throw new ArgumentException($"contactId invÃ¡lido: '{contactIdString}'. Deve ser um GUID vÃ¡lido. Use a funÃ§Ã£o search_contacts para encontrar o contato por nome primeiro.");

        var text = textProp.GetString();
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("text nÃ£o pode ser vazio");

        var command = new CreateTextNoteCommand
        {
            ContactId = contactId,
            AuthorId = ownerUserId,
            Text = text,
            StructuredData = args.TryGetProperty("structuredData", out var structProp) && structProp.ValueKind != JsonValueKind.Null
                ? structProp.GetString()
                : null
        };

        var noteId = await _mediator.Send(command, cancellationToken);
        return new { noteId };
    }

    private async Task<object> ExecuteUpdateNoteAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("id", out var idProp))
            throw new ArgumentException("id Ã© obrigatÃ³rio");

        var command = new UpdateNoteCommand
        {
            NoteId = Guid.Parse(idProp.GetString() ?? ""),
            OwnerUserId = ownerUserId,
            RawContent = args.TryGetProperty("rawContent", out var contentProp) ? contentProp.GetString() : null,
            StructuredData = args.TryGetProperty("structuredData", out var structProp) ? structProp.GetString() : null
        };

        await _mediator.Send(command, cancellationToken);
        return new { success = true };
    }

    private async Task<object> ExecuteDeleteNoteAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("id", out var idProp))
            throw new ArgumentException("id Ã© obrigatÃ³rio");

        var command = new DeleteNoteCommand
        {
            NoteId = Guid.Parse(idProp.GetString() ?? ""),
            OwnerUserId = ownerUserId
        };

        await _mediator.Send(command, cancellationToken);
        return new { success = true };
    }

    // ========== FunÃ§Ãµes de Drafts ==========

    private async Task<object> ExecuteCreateDraftAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("documentType", out var typeProp) || !args.TryGetProperty("content", out var contentProp))
            throw new ArgumentException("documentType e content sÃ£o obrigatÃ³rios");

        if (!Enum.TryParse<DocumentType>(typeProp.GetString(), out var docType))
            throw new ArgumentException("documentType invÃ¡lido");

        var command = new CreateDraftDocumentCommand
        {
            OwnerUserId = ownerUserId,
            DocumentType = docType,
            Content = contentProp.GetString() ?? "",
            ContactId = args.TryGetProperty("contactId", out var contactProp) && contactProp.ValueKind != JsonValueKind.Null
                ? Guid.Parse(contactProp.GetString() ?? "")
                : null,
            CompanyId = args.TryGetProperty("companyId", out var companyProp) && companyProp.ValueKind != JsonValueKind.Null
                ? Guid.Parse(companyProp.GetString() ?? "")
                : null,
            TemplateId = args.TryGetProperty("templateId", out var templateProp) && templateProp.ValueKind != JsonValueKind.Null
                ? Guid.Parse(templateProp.GetString() ?? "")
                : null,
            LetterheadId = args.TryGetProperty("letterheadId", out var letterProp) && letterProp.ValueKind != JsonValueKind.Null
                ? Guid.Parse(letterProp.GetString() ?? "")
                : null
        };

        var draftId = await _mediator.Send(command, cancellationToken);
        return new { draftId };
    }

    private async Task<object> ExecuteListDraftsAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        var query = new ListDraftsQuery
        {
            OwnerUserId = ownerUserId,
            ContactId = args.TryGetProperty("contactId", out var contactProp) && contactProp.ValueKind != JsonValueKind.Null
                ? Guid.Parse(contactProp.GetString() ?? "")
                : null,
            CompanyId = args.TryGetProperty("companyId", out var companyProp) && companyProp.ValueKind != JsonValueKind.Null
                ? Guid.Parse(companyProp.GetString() ?? "")
                : null,
            DocumentType = args.TryGetProperty("documentType", out var typeProp) && typeProp.ValueKind != JsonValueKind.Null
                ? Enum.Parse<DocumentType>(typeProp.GetString() ?? "")
                : null,
            Status = args.TryGetProperty("status", out var statusProp) && statusProp.ValueKind != JsonValueKind.Null
                ? Enum.Parse<DraftStatus>(statusProp.GetString() ?? "")
                : null,
            Page = args.TryGetProperty("page", out var pageProp) ? pageProp.GetInt32() : 1,
            PageSize = args.TryGetProperty("pageSize", out var pageSizeProp) ? pageSizeProp.GetInt32() : 20
        };

        var result = await _mediator.Send(query, cancellationToken);
        return result;
    }

    private async Task<object> ExecuteGetDraftAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("id", out var idProp))
            throw new ArgumentException("id Ã© obrigatÃ³rio");

        var query = new GetDraftByIdQuery
        {
            DraftId = Guid.Parse(idProp.GetString() ?? ""),
            OwnerUserId = ownerUserId
        };

        var result = await _mediator.Send(query, cancellationToken);
        if (result == null)
            throw new InvalidOperationException("Draft nÃ£o encontrado");

        return result;
    }

    private async Task<object> ExecuteUpdateDraftAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("id", out var idProp))
            throw new ArgumentException("id Ã© obrigatÃ³rio");

        var command = new UpdateDraftDocumentCommand
        {
            DraftId = Guid.Parse(idProp.GetString() ?? ""),
            OwnerUserId = ownerUserId,
            Content = args.TryGetProperty("content", out var contentProp) ? contentProp.GetString() : null,
            ContactId = args.TryGetProperty("contactId", out var contactProp) && contactProp.ValueKind != JsonValueKind.Null
                ? Guid.Parse(contactProp.GetString() ?? "")
                : null,
            CompanyId = args.TryGetProperty("companyId", out var companyProp) && companyProp.ValueKind != JsonValueKind.Null
                ? Guid.Parse(companyProp.GetString() ?? "")
                : null,
            TemplateId = args.TryGetProperty("templateId", out var templateProp) && templateProp.ValueKind != JsonValueKind.Null
                ? Guid.Parse(templateProp.GetString() ?? "")
                : null,
            LetterheadId = args.TryGetProperty("letterheadId", out var letterProp) && letterProp.ValueKind != JsonValueKind.Null
                ? Guid.Parse(letterProp.GetString() ?? "")
                : null
        };

        await _mediator.Send(command, cancellationToken);
        return new { success = true };
    }

    private async Task<object> ExecuteApproveDraftAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("id", out var idProp))
            throw new ArgumentException("id Ã© obrigatÃ³rio");

        var command = new ApproveDraftCommand
        {
            DraftId = Guid.Parse(idProp.GetString() ?? ""),
            OwnerUserId = ownerUserId
        };

        await _mediator.Send(command, cancellationToken);
        return new { success = true };
    }

    private async Task<object> ExecuteSendDraftAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("id", out var idProp))
            throw new ArgumentException("id Ã© obrigatÃ³rio");

        var command = new SendDraftCommand
        {
            DraftId = Guid.Parse(idProp.GetString() ?? ""),
            OwnerUserId = ownerUserId
        };

        await _mediator.Send(command, cancellationToken);
        return new { success = true };
    }

    private async Task<object> ExecuteDeleteDraftAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("id", out var idProp))
            throw new ArgumentException("id Ã© obrigatÃ³rio");

        var command = new DeleteDraftCommand
        {
            DraftId = Guid.Parse(idProp.GetString() ?? ""),
            OwnerUserId = ownerUserId
        };

        await _mediator.Send(command, cancellationToken);
        return new { success = true };
    }

    // ========== FunÃ§Ãµes de Templates ==========

    private async Task<object> ExecuteCreateTemplateAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("name", out var nameProp) || !args.TryGetProperty("type", out var typeProp) || !args.TryGetProperty("body", out var bodyProp))
            throw new ArgumentException("name, type e body sÃ£o obrigatÃ³rios");

        if (!Enum.TryParse<TemplateType>(typeProp.GetString(), out var templateType))
            throw new ArgumentException("type invÃ¡lido");

        var command = new CreateTemplateCommand
        {
            OwnerUserId = ownerUserId,
            Name = nameProp.GetString() ?? "",
            Type = templateType,
            Body = bodyProp.GetString() ?? "",
            PlaceholdersSchema = args.TryGetProperty("placeholdersSchema", out var schemaProp) ? schemaProp.GetString() : null
        };

        var templateId = await _mediator.Send(command, cancellationToken);
        return new { templateId };
    }

    private async Task<object> ExecuteListTemplatesAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        var query = new ListTemplatesQuery
        {
            OwnerUserId = ownerUserId,
            Type = args.TryGetProperty("type", out var typeProp) && typeProp.ValueKind != JsonValueKind.Null
                ? Enum.Parse<TemplateType>(typeProp.GetString() ?? "")
                : null,
            ActiveOnly = args.TryGetProperty("activeOnly", out var activeProp) && activeProp.GetBoolean(),
            Page = args.TryGetProperty("page", out var pageProp) ? pageProp.GetInt32() : 1,
            PageSize = args.TryGetProperty("pageSize", out var pageSizeProp) ? pageSizeProp.GetInt32() : 20
        };

        var result = await _mediator.Send(query, cancellationToken);
        return result;
    }

    private async Task<object> ExecuteGetTemplateAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("id", out var idProp))
            throw new ArgumentException("id Ã© obrigatÃ³rio");

        var query = new GetTemplateByIdQuery
        {
            TemplateId = Guid.Parse(idProp.GetString() ?? ""),
            OwnerUserId = ownerUserId
        };

        var result = await _mediator.Send(query, cancellationToken);
        if (result == null)
            throw new InvalidOperationException("Template nÃ£o encontrado");

        return result;
    }

    private async Task<object> ExecuteUpdateTemplateAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("id", out var idProp))
            throw new ArgumentException("id Ã© obrigatÃ³rio");

        var command = new UpdateTemplateCommand
        {
            TemplateId = Guid.Parse(idProp.GetString() ?? ""),
            OwnerUserId = ownerUserId,
            Name = args.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null,
            Body = args.TryGetProperty("body", out var bodyProp) ? bodyProp.GetString() : null,
            PlaceholdersSchema = args.TryGetProperty("placeholdersSchema", out var schemaProp) ? schemaProp.GetString() : null,
            Active = args.TryGetProperty("active", out var activeProp) ? activeProp.GetBoolean() : null
        };

        await _mediator.Send(command, cancellationToken);
        return new { success = true };
    }

    private async Task<object> ExecuteDeleteTemplateAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("id", out var idProp))
            throw new ArgumentException("id Ã© obrigatÃ³rio");

        var command = new DeleteTemplateCommand
        {
            TemplateId = Guid.Parse(idProp.GetString() ?? ""),
            OwnerUserId = ownerUserId
        };

        await _mediator.Send(command, cancellationToken);
        return new { success = true };
    }

    // ========== FunÃ§Ãµes de Letterheads ==========

    private async Task<object> ExecuteCreateLetterheadAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("name", out var nameProp) || !args.TryGetProperty("designData", out var designProp))
            throw new ArgumentException("name e designData sÃ£o obrigatÃ³rios");

        var command = new CreateLetterheadCommand
        {
            OwnerUserId = ownerUserId,
            Name = nameProp.GetString() ?? "",
            DesignData = designProp.GetString() ?? ""
        };

        var letterheadId = await _mediator.Send(command, cancellationToken);
        return new { letterheadId };
    }

    private async Task<object> ExecuteListLetterheadsAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        var query = new ListLetterheadsQuery
        {
            OwnerUserId = ownerUserId,
            ActiveOnly = args.TryGetProperty("activeOnly", out var activeProp) && activeProp.GetBoolean(),
            Page = args.TryGetProperty("page", out var pageProp) ? pageProp.GetInt32() : 1,
            PageSize = args.TryGetProperty("pageSize", out var pageSizeProp) ? pageSizeProp.GetInt32() : 20
        };

        var result = await _mediator.Send(query, cancellationToken);
        return result;
    }

    private async Task<object> ExecuteGetLetterheadAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("id", out var idProp))
            throw new ArgumentException("id Ã© obrigatÃ³rio");

        var query = new GetLetterheadByIdQuery
        {
            LetterheadId = Guid.Parse(idProp.GetString() ?? ""),
            OwnerUserId = ownerUserId
        };

        var result = await _mediator.Send(query, cancellationToken);
        if (result == null)
            throw new InvalidOperationException("Letterhead nÃ£o encontrado");

        return result;
    }

    private async Task<object> ExecuteUpdateLetterheadAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("id", out var idProp))
            throw new ArgumentException("id Ã© obrigatÃ³rio");

        var command = new UpdateLetterheadCommand
        {
            LetterheadId = Guid.Parse(idProp.GetString() ?? ""),
            OwnerUserId = ownerUserId,
            Name = args.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null,
            DesignData = args.TryGetProperty("designData", out var designProp) ? designProp.GetString() : null,
            IsActive = args.TryGetProperty("isActive", out var activeProp) ? activeProp.GetBoolean() : null
        };

        await _mediator.Send(command, cancellationToken);
        return new { success = true };
    }

    private async Task<object> ExecuteDeleteLetterheadAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("id", out var idProp))
            throw new ArgumentException("id Ã© obrigatÃ³rio");

        var command = new DeleteLetterheadCommand
        {
            LetterheadId = Guid.Parse(idProp.GetString() ?? ""),
            OwnerUserId = ownerUserId
        };

        await _mediator.Send(command, cancellationToken);
        return new { success = true };
    }

    // ========== Workflow Functions ==========

    private async Task<object> ExecuteCreateWorkflowAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("specJson", out var specJsonProp))
            throw new ArgumentException("specJson Ã© obrigatÃ³rio");

        var activateImmediately = args.TryGetProperty("activateImmediately", out var activateProp) && activateProp.ValueKind == JsonValueKind.True;

        var command = new Commands.Workflow.CreateWorkflowFromSpecCommand
        {
            OwnerUserId = ownerUserId,
            SpecJson = specJsonProp.GetString() ?? "",
            ActivateImmediately = activateImmediately
        };

        var result = await _mediator.Send(command, cancellationToken);

        // Return a clear response format for the LLM
        if (!result.Success)
        {
            _logger.LogWarning("Workflow creation failed: {Errors}", string.Join(", ", result.Errors));
            return new
            {
                success = false,
                error = string.Join("; ", result.Errors),
                message = "Falha ao criar o workflow. Verifique os erros acima."
            };
        }

        return new
        {
            success = true,
            workflowId = result.WorkflowId,
            n8nWorkflowId = result.N8nWorkflowId,
            message = "Workflow criado com sucesso!"
        };
    }

    private async Task<object> ExecuteListWorkflowsAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        Domain.Enums.WorkflowStatus? status = null;
        if (args.TryGetProperty("status", out var statusProp) && statusProp.ValueKind == JsonValueKind.String)
        {
            if (Enum.TryParse<Domain.Enums.WorkflowStatus>(statusProp.GetString(), out var parsedStatus))
                status = parsedStatus;
        }

        var query = new Queries.Workflow.ListWorkflowsQuery
        {
            OwnerUserId = ownerUserId,
            FilterByStatus = status
        };

        return await _mediator.Send(query, cancellationToken);
    }

    private async Task<object> ExecuteGetWorkflowAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("id", out var idProp))
            throw new ArgumentException("id Ã© obrigatÃ³rio");

        var query = new Queries.Workflow.GetWorkflowByIdQuery
        {
            WorkflowId = Guid.Parse(idProp.GetString() ?? ""),
            OwnerUserId = ownerUserId
        };

        var result = await _mediator.Send(query, cancellationToken);
        if (result == null)
            return new { error = "Workflow nÃ£o encontrado" };
        return result;
    }

    private async Task<object> ExecuteRunWorkflowAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("id", out var idProp))
            throw new ArgumentException("id Ã© obrigatÃ³rio");

        string? inputJson = null;
        if (args.TryGetProperty("inputJson", out var inputProp) && inputProp.ValueKind == JsonValueKind.String)
            inputJson = inputProp.GetString();

        var command = new Commands.Workflow.ExecuteWorkflowCommand
        {
            WorkflowId = Guid.Parse(idProp.GetString() ?? ""),
            OwnerUserId = ownerUserId,
            InputJson = inputJson
        };

        return await _mediator.Send(command, cancellationToken);
    }

    private async Task<object> ExecuteApproveWorkflowStepAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("executionId", out var idProp))
            throw new ArgumentException("executionId Ã© obrigatÃ³rio");

        var command = new Commands.Workflow.ApproveWorkflowStepCommand
        {
            ExecutionId = Guid.Parse(idProp.GetString() ?? ""),
            ApprovedByUserId = ownerUserId
        };

        return await _mediator.Send(command, cancellationToken);
    }

    private async Task<object> ExecuteListWorkflowExecutionsAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        Guid? workflowId = null;
        if (args.TryGetProperty("workflowId", out var workflowIdProp) && workflowIdProp.ValueKind == JsonValueKind.String)
            workflowId = Guid.Parse(workflowIdProp.GetString() ?? "");

        var limit = args.TryGetProperty("limit", out var limitProp) && limitProp.ValueKind == JsonValueKind.Number
            ? limitProp.GetInt32()
            : 50;

        var query = new Queries.Workflow.ListWorkflowExecutionsQuery
        {
            OwnerUserId = ownerUserId,
            WorkflowId = workflowId,
            Limit = limit
        };

        return await _mediator.Send(query, cancellationToken);
    }

    private async Task<object> ExecuteGetPendingApprovalsAsync(Guid ownerUserId, CancellationToken cancellationToken)
    {
        var query = new Queries.Workflow.GetPendingApprovalsQuery
        {
            OwnerUserId = ownerUserId
        };

        return await _mediator.Send(query, cancellationToken);
    }

    private async Task<string?> GetWorkflowPromptAsync(CancellationToken cancellationToken)
    {
        try
        {
            var config = await _mediator.Send(new GetAgentConfigurationQuery(), cancellationToken);
            var prompt = config?.WorkflowPrompt;
            return string.IsNullOrWhiteSpace(prompt) ? null : prompt.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load workflow prompt context");
            return null;
        }
    }

    private List<object> GetFunctionDefinitions(string? workflowPromptContext)
    {
        var workflowContextSuffix = string.IsNullOrWhiteSpace(workflowPromptContext)
            ? ""
            : $" Contexto adicional: {workflowPromptContext}";

        return new List<object>
        {
            // ========= Contatos =========
            new
            {
                type = "function",
                name = "list_contacts",
                description = "Lista todos os contatos do usuÃ¡rio autenticado. Suporta paginaÃ§Ã£o e filtros.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        page = new { type = new[] { "number", "null" }, description = "NÃºmero da pÃ¡gina (padrÃ£o: 1)" },
                        pageSize = new { type = new[] { "number", "null" }, description = "Tamanho da pÃ¡gina (padrÃ£o: 20)" },
                        includeDeleted = new { type = new[] { "boolean", "null" }, description = "Incluir contatos deletados (padrÃ£o: false)" }
                    },
                    required = new[] { "page", "pageSize", "includeDeleted" },
                    additionalProperties = false
                },
                strict = true
            },
            new
            {
                type = "function",
                name = "search_contacts",
                description = "Busca contatos com filtro de texto. Ãštil para encontrar contatos por nome, empresa, email, etc.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        searchTerm = new { type = "string", description = "Termo de busca (nome, empresa, email, etc)" },
                        page = new { type = new[] { "number", "null" }, description = "NÃºmero da pÃ¡gina (padrÃ£o: 1)" },
                        pageSize = new { type = new[] { "number", "null" }, description = "Tamanho da pÃ¡gina (padrÃ£o: 20)" }
                    },
                    required = new[] { "searchTerm", "page", "pageSize" },
                    additionalProperties = false
                },
                strict = true
            },
            new
            {
                type = "function",
                name = "get_contact",
                description = "ObtÃ©m um contato especÃ­fico por ID",
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
            },
            new
            {
                type = "function",
                name = "create_contact",
                description = "Cria um novo contato. Primeiro nome Ã© obrigatÃ³rio, outros campos sÃ£o opcionais.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        firstName = new { type = "string", description = "Primeiro nome (obrigatÃ³rio)" },
                        lastName = new { type = new[] { "string", "null" }, description = "Sobrenome" },
                        jobTitle = new { type = new[] { "string", "null" }, description = "Cargo" },
                        company = new { type = new[] { "string", "null" }, description = "Empresa" },
                        street = new { type = new[] { "string", "null" }, description = "EndereÃ§o (rua)" },
                        city = new { type = new[] { "string", "null" }, description = "Cidade" },
                        state = new { type = new[] { "string", "null" }, description = "Estado" },
                        zipCode = new { type = new[] { "string", "null" }, description = "CEP" },
                        country = new { type = new[] { "string", "null" }, description = "PaÃ­s" }
                    },
                    required = new[] { "firstName", "lastName", "jobTitle", "company", "street", "city", "state", "zipCode", "country" },
                    additionalProperties = false
                },
                strict = true
            },
            // ========= Lembretes =========
            new
            {
                type = "function",
                name = "create_reminder",
                description = "Cria um novo lembrete para um contato. Ãštil para agendar follow-ups ou tarefas. IMPORTANTE: Se vocÃª nÃ£o tiver o contactId (GUID) do contato, use primeiro a funÃ§Ã£o search_contacts para encontrar o contato pelo nome e obter seu ID.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        contactId = new { type = "string", description = "ID do contato (GUID vÃ¡lido). Se vocÃª nÃ£o tiver o GUID, use search_contacts primeiro para encontrar o contato pelo nome." },
                        reason = new { type = "string", description = "Motivo do lembrete (mÃ¡ximo 500 caracteres)" },
                        suggestedMessage = new { type = new[] { "string", "null" }, description = "Mensagem sugerida para o lembrete (mÃ¡ximo 2000 caracteres, opcional)" },
                        scheduledFor = new { type = "string", description = "Data e hora agendada (formato ISO 8601, ex: 2024-12-25T10:00:00Z)" }
                    },
                    required = new[] { "contactId", "reason", "scheduledFor", "suggestedMessage" },
                    additionalProperties = false
                },
                strict = true
            },
            new
            {
                type = "function",
                name = "list_reminders",
                description = "Lista lembretes do usuÃ¡rio autenticado. Suporta filtros por contato, status e data.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        contactId = new { type = new[] { "string", "null" }, description = "Filtrar por ID do contato (GUID)" },
                        status = new { type = new[] { "string", "null" }, description = "Filtrar por status (Pending, Sent, Dismissed, Snoozed)", @enum = new[] { "Pending", "Sent", "Dismissed", "Snoozed" } },
                        startDate = new { type = new[] { "string", "null" }, description = "Data inicial (formato ISO 8601)" },
                        endDate = new { type = new[] { "string", "null" }, description = "Data final (formato ISO 8601)" },
                        page = new { type = new[] { "number", "null" }, description = "NÃºmero da pÃ¡gina (padrÃ£o: 1)" },
                        pageSize = new { type = new[] { "number", "null" }, description = "Tamanho da pÃ¡gina (padrÃ£o: 20)" }
                    },
                    required = new[] { "contactId", "status", "startDate", "endDate", "page", "pageSize" },
                    additionalProperties = false
                },
                strict = true
            },
            new
            {
                type = "function",
                name = "get_reminder",
                description = "ObtÃ©m um lembrete especÃ­fico por ID",
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
            },
            new
            {
                type = "function",
                name = "update_reminder_status",
                description = "Atualiza o status de um lembrete (Pending, Completed, Cancelled)",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        id = new { type = "string", description = "ID do lembrete (GUID)" },
                        newStatus = new { type = "string", description = "Novo status", @enum = new[] { "Pending", "Sent", "Dismissed", "Snoozed" } },
                        newScheduledFor = new { type = new[] { "string", "null" }, description = "Nova data agendada (formato ISO 8601, opcional)" }
                    },
                    required = new[] { "id", "newStatus", "newScheduledFor" },
                    additionalProperties = false
                },
                strict = true
            },
            new
            {
                type = "function",
                name = "delete_reminder",
                description = "Deleta um lembrete",
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
            },
            // ========= Notas =========
            new
            {
                type = "function",
                name = "list_contact_notes",
                description = "Lista todas as notas de um contato especÃ­fico",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        contactId = new { type = "string", description = "ID do contato (GUID)" }
                    },
                    required = new[] { "contactId" },
                    additionalProperties = false
                },
                strict = true
            },
            new
            {
                type = "function",
                name = "get_note",
                description = "ObtÃ©m uma nota especÃ­fica por ID",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        id = new { type = "string", description = "ID da nota (GUID)" }
                    },
                    required = new[] { "id" },
                    additionalProperties = false
                },
                strict = true
            },
            new
            {
                type = "function",
                name = "create_text_note",
                description = "Cria uma nova nota de texto para um contato. IMPORTANTE: Se vocÃª nÃ£o tiver o contactId (GUID) do contato, use primeiro a funÃ§Ã£o search_contacts para encontrar o contato pelo nome e obter seu ID.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        contactId = new { type = "string", description = "ID do contato (GUID). Se vocÃª nÃ£o tiver o GUID, use search_contacts primeiro para encontrar o contato pelo nome." },
                        text = new { type = "string", description = "ConteÃºdo da nota" },
                        structuredData = new { type = new[] { "string", "null" }, description = "Dados estruturados em JSON (opcional)" }
                    },
                    required = new[] { "contactId", "text", "structuredData" },
                    additionalProperties = false
                },
                strict = true
            },
            new
            {
                type = "function",
                name = "update_note",
                description = "Atualiza uma nota existente",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        id = new { type = "string", description = "ID da nota (GUID)" },
                        rawContent = new { type = new[] { "string", "null" }, description = "ConteÃºdo bruto da nota" },
                        structuredData = new { type = new[] { "string", "null" }, description = "Dados estruturados em JSON" }
                    },
                    required = new[] { "id", "rawContent", "structuredData" },
                    additionalProperties = false
                },
                strict = true
            },
            new
            {
                type = "function",
                name = "delete_note",
                description = "Deleta uma nota",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        id = new { type = "string", description = "ID da nota (GUID)" }
                    },
                    required = new[] { "id" },
                    additionalProperties = false
                },
                strict = true
            },
            // ========= Drafts =========
            new
            {
                type = "function",
                name = "create_draft",
                description = "Cria um novo draft de documento",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        documentType = new { type = "string", description = "Tipo de documento", @enum = new[] { "Email", "Oficio", "Invite" } },
                        content = new { type = "string", description = "ConteÃºdo do documento" },
                        contactId = new { type = new[] { "string", "null" }, description = "ID do contato relacionado (GUID, opcional)" },
                        companyId = new { type = new[] { "string", "null" }, description = "ID da empresa relacionada (GUID, opcional)" },
                        templateId = new { type = new[] { "string", "null" }, description = "ID do template usado (GUID, opcional)" },
                        letterheadId = new { type = new[] { "string", "null" }, description = "ID do papel timbrado usado (GUID, opcional)" }
                    },
                    required = new[] { "documentType", "content", "contactId", "companyId", "templateId", "letterheadId" },
                    additionalProperties = false
                },
                strict = true
            },
            new
            {
                type = "function",
                name = "list_drafts",
                description = "Lista drafts do usuÃ¡rio autenticado",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        contactId = new { type = new[] { "string", "null" }, description = "Filtrar por ID do contato (GUID)" },
                        companyId = new { type = new[] { "string", "null" }, description = "Filtrar por ID da empresa (GUID)" },
                        documentType = new { type = new[] { "string", "null" }, description = "Filtrar por tipo de documento", @enum = new[] { "Email", "Oficio", "Invite" } },
                        status = new { type = new[] { "string", "null" }, description = "Filtrar por status", @enum = new[] { "Draft", "Approved", "Sent" } },
                        page = new { type = new[] { "number", "null" }, description = "NÃºmero da pÃ¡gina (padrÃ£o: 1)" },
                        pageSize = new { type = new[] { "number", "null" }, description = "Tamanho da pÃ¡gina (padrÃ£o: 20)" }
                    },
                    required = new[] { "contactId", "companyId", "documentType", "status", "page", "pageSize" },
                    additionalProperties = false
                },
                strict = true
            },
            new
            {
                type = "function",
                name = "get_draft",
                description = "ObtÃ©m um draft especÃ­fico por ID",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        id = new { type = "string", description = "ID do draft (GUID)" }
                    },
                    required = new[] { "id" },
                    additionalProperties = false
                },
                strict = true
            },
            new
            {
                type = "function",
                name = "update_draft",
                description = "Atualiza um draft",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        id = new { type = "string", description = "ID do draft (GUID)" },
                        content = new { type = new[] { "string", "null" }, description = "ConteÃºdo do documento" },
                        contactId = new { type = new[] { "string", "null" }, description = "ID do contato relacionado (GUID)" },
                        companyId = new { type = new[] { "string", "null" }, description = "ID da empresa relacionada (GUID)" },
                        templateId = new { type = new[] { "string", "null" }, description = "ID do template usado (GUID)" },
                        letterheadId = new { type = new[] { "string", "null" }, description = "ID do papel timbrado usado (GUID)" }
                    },
                    required = new[] { "id", "content", "contactId", "companyId", "templateId", "letterheadId" },
                    additionalProperties = false
                },
                strict = true
            },
            new
            {
                type = "function",
                name = "approve_draft",
                description = "Aprova um draft",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        id = new { type = "string", description = "ID do draft (GUID)" }
                    },
                    required = new[] { "id" },
                    additionalProperties = false
                },
                strict = true
            },
            new
            {
                type = "function",
                name = "send_draft",
                description = "Envia um draft",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        id = new { type = "string", description = "ID do draft (GUID)" }
                    },
                    required = new[] { "id" },
                    additionalProperties = false
                },
                strict = true
            },
            new
            {
                type = "function",
                name = "delete_draft",
                description = "Deleta um draft",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        id = new { type = "string", description = "ID do draft (GUID)" }
                    },
                    required = new[] { "id" },
                    additionalProperties = false
                },
                strict = true
            },
            // ========= Templates =========
            new
            {
                type = "function",
                name = "create_template",
                description = "Cria um novo template de documento",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        name = new { type = "string", description = "Nome do template (mÃ¡ximo 200 caracteres)" },
                        type = new { type = "string", description = "Tipo de template", @enum = new[] { "Email", "Oficio", "Invite", "Generic" } },
                        body = new { type = "string", description = "Corpo do template" },
                        placeholdersSchema = new { type = new[] { "string", "null" }, description = "Schema JSON dos placeholders (opcional)" }
                    },
                    required = new[] { "name", "type", "body", "placeholdersSchema" },
                    additionalProperties = false
                },
                strict = true
            },
            new
            {
                type = "function",
                name = "list_templates",
                description = "Lista templates do usuÃ¡rio autenticado",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        type = new { type = new[] { "string", "null" }, description = "Filtrar por tipo", @enum = new[] { "Email", "Oficio", "Invite", "Generic" } },
                        activeOnly = new { type = new[] { "boolean", "null" }, description = "Apenas templates ativos (padrÃ£o: false)" },
                        page = new { type = new[] { "number", "null" }, description = "NÃºmero da pÃ¡gina (padrÃ£o: 1)" },
                        pageSize = new { type = new[] { "number", "null" }, description = "Tamanho da pÃ¡gina (padrÃ£o: 20)" }
                    },
                    required = new[] { "type", "activeOnly", "page", "pageSize" },
                    additionalProperties = false
                },
                strict = true
            },
            new
            {
                type = "function",
                name = "get_template",
                description = "ObtÃ©m um template especÃ­fico por ID",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        id = new { type = "string", description = "ID do template (GUID)" }
                    },
                    required = new[] { "id" },
                    additionalProperties = false
                },
                strict = true
            },
            new
            {
                type = "function",
                name = "update_template",
                description = "Atualiza um template",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        id = new { type = "string", description = "ID do template (GUID)" },
                        name = new { type = new[] { "string", "null" }, description = "Nome do template" },
                        body = new { type = new[] { "string", "null" }, description = "Corpo do template" },
                        placeholdersSchema = new { type = new[] { "string", "null" }, description = "Schema JSON dos placeholders" },
                        active = new { type = new[] { "boolean", "null" }, description = "Se o template estÃ¡ ativo" }
                    },
                    required = new[] { "id", "name", "body", "placeholdersSchema", "active" },
                    additionalProperties = false
                },
                strict = true
            },
            new
            {
                type = "function",
                name = "delete_template",
                description = "Deleta um template",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        id = new { type = "string", description = "ID do template (GUID)" }
                    },
                    required = new[] { "id" },
                    additionalProperties = false
                },
                strict = true
            },
            // ========= Letterheads =========
            new
            {
                type = "function",
                name = "create_letterhead",
                description = "Cria um novo papel timbrado",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        name = new { type = "string", description = "Nome do papel timbrado (mÃ¡ximo 200 caracteres)" },
                        designData = new { type = "string", description = "Dados de design em JSON" }
                    },
                    required = new[] { "name", "designData" },
                    additionalProperties = false
                },
                strict = true
            },
            new
            {
                type = "function",
                name = "list_letterheads",
                description = "Lista papÃ©is timbrados do usuÃ¡rio autenticado",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        activeOnly = new { type = new[] { "boolean", "null" }, description = "Apenas papÃ©is timbrados ativos (padrÃ£o: false)" },
                        page = new { type = new[] { "number", "null" }, description = "NÃºmero da pÃ¡gina (padrÃ£o: 1)" },
                        pageSize = new { type = new[] { "number", "null" }, description = "Tamanho da pÃ¡gina (padrÃ£o: 20)" }
                    },
                    required = new[] { "activeOnly", "page", "pageSize" },
                    additionalProperties = false
                },
                strict = true
            },
            new
            {
                type = "function",
                name = "get_letterhead",
                description = "ObtÃ©m um papel timbrado especÃ­fico por ID",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        id = new { type = "string", description = "ID do papel timbrado (GUID)" }
                    },
                    required = new[] { "id" },
                    additionalProperties = false
                },
                strict = true
            },
            new
            {
                type = "function",
                name = "update_letterhead",
                description = "Atualiza um papel timbrado",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        id = new { type = "string", description = "ID do papel timbrado (GUID)" },
                        name = new { type = new[] { "string", "null" }, description = "Nome do papel timbrado" },
                        designData = new { type = new[] { "string", "null" }, description = "Dados de design em JSON" },
                        isActive = new { type = new[] { "boolean", "null" }, description = "Se o papel timbrado estÃ¡ ativo" }
                    },
                    required = new[] { "id", "name", "designData", "isActive" },
                    additionalProperties = false
                },
                strict = true
            },
            new
            {
                type = "function",
                name = "delete_letterhead",
                description = "Deleta um papel timbrado",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        id = new { type = "string", description = "ID do papel timbrado (GUID)" }
                    },
                    required = new[] { "id" },
                    additionalProperties = false
                },
                strict = true
            },
            // ========= Workflows =========
            new
            {
                type = "function",
                name = "create_workflow",
                description = "Cria um novo workflow de automacao a partir de um WorkflowSpec JSON. O workflow deve ter um trigger inicial do tipo Webhook, Scheduled ou EventBased (Manual nao e permitido)." + workflowContextSuffix,
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        specJson = new { type = "string", description = "JSON do WorkflowSpec contendo: name, description, trigger (type: Webhook/Scheduled/EventBased; para Webhook informe eventName como path), variables, steps (cada step tem id, name, type: Action/Condition, action ou condition)." },
                        activateImmediately = new { type = new[] { "boolean", "null" }, description = "Se true, ativa o workflow imediatamente apÃ³s criaÃ§Ã£o (padrÃ£o: false)" }
                    },
                    required = new[] { "specJson", "activateImmediately" },
                    additionalProperties = false
                },
                strict = true
            },
            new
            {
                type = "function",
                name = "list_workflows",
                description = "Lista todos os workflows do usuÃ¡rio autenticado",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        status = new { type = new[] { "string", "null" }, description = "Filtrar por status", @enum = new[] { "Draft", "Active", "Paused", "Archived" } }
                    },
                    required = new[] { "status" },
                    additionalProperties = false
                },
                strict = true
            },
            new
            {
                type = "function",
                name = "get_workflow",
                description = "ObtÃ©m detalhes de um workflow especÃ­fico por ID",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        id = new { type = "string", description = "ID do workflow (GUID)" }
                    },
                    required = new[] { "id" },
                    additionalProperties = false
                },
                strict = true
            },
            new
            {
                type = "function",
                name = "run_workflow",
                description = "Executa um workflow. O workflow deve estar ativo para ser executado.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        id = new { type = "string", description = "ID do workflow (GUID)" },
                        inputJson = new { type = new[] { "string", "null" }, description = "JSON com os inputs para o workflow (opcional)" }
                    },
                    required = new[] { "id", "inputJson" },
                    additionalProperties = false
                },
                strict = true
            },
            new
            {
                type = "function",
                name = "approve_workflow_step",
                description = "Aprova um step de workflow que estÃ¡ aguardando aprovaÃ§Ã£o",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        executionId = new { type = "string", description = "ID da execuÃ§Ã£o (GUID)" }
                    },
                    required = new[] { "executionId" },
                    additionalProperties = false
                },
                strict = true
            },
            new
            {
                type = "function",
                name = "list_workflow_executions",
                description = "Lista o histÃ³rico de execuÃ§Ãµes de workflows",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        workflowId = new { type = new[] { "string", "null" }, description = "Filtrar por ID de workflow especÃ­fico (GUID)" },
                        limit = new { type = new[] { "number", "null" }, description = "Limite de resultados (padrÃ£o: 50)" }
                    },
                    required = new[] { "workflowId", "limit" },
                    additionalProperties = false
                },
                strict = true
            },
            new
            {
                type = "function",
                name = "get_pending_approvals",
                description = "Lista execuÃ§Ãµes de workflow aguardando aprovaÃ§Ã£o do usuÃ¡rio",
                parameters = new
                {
                    type = "object",
                    properties = new Dictionary<string, object>(),
                    required = Array.Empty<string>(),
                    additionalProperties = false
                },
                strict = true
            }
        };
    }
    // Helpers para datas (garantir UTC)
    private static DateTime? ParseOptionalUtc(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dto))
            throw new ArgumentException($"Data/hora invalida: '{value}'. Deve estar no formato ISO 8601 (ex: 2024-12-25T10:00:00Z)");

        return dto.UtcDateTime;
    }

}

// Classes auxiliares para deserializaÃ§Ã£o da resposta do OpenAI Responses API
// Usar JsonPropertyName para mapear corretamente os campos do JSON
public class OpenAIResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("output")]
    public List<OutputItem>? Output { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string? Id { get; set; }
}

public class OutputItem
{
    [System.Text.Json.Serialization.JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string? Id { get; set; }

    // Para tipo "text"
    [System.Text.Json.Serialization.JsonPropertyName("text")]
    public string? Text { get; set; }

    // Para tipo "function_call" (campos diretos na Responses API)
    [System.Text.Json.Serialization.JsonPropertyName("call_id")]
    public string? CallId { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string? Name { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("arguments")]
    public string? Arguments { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("output")]
    public string? Output { get; set; }

    // Para tipo "message" - content Ã© um array de ContentPart
    [System.Text.Json.Serialization.JsonPropertyName("content")]
    public List<ContentPart>? Content { get; set; }
}

public class ContentPart
{
    [System.Text.Json.Serialization.JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("text")]
    public string? Text { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("annotations")]
    public List<object>? Annotations { get; set; }
}

// Input items tipados para Responses API
public class InputMessage
{
    [System.Text.Json.Serialization.JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

public class InputFunctionCall
{
    [System.Text.Json.Serialization.JsonPropertyName("type")]
    public string Type { get; } = "function_call";

    [System.Text.Json.Serialization.JsonPropertyName("call_id")]
    public string CallId { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("arguments")]
    public string Arguments { get; set; } = string.Empty;
}

public class InputFunctionCallOutput
{
    [System.Text.Json.Serialization.JsonPropertyName("type")]
    public string Type { get; } = "function_call_output";

    [System.Text.Json.Serialization.JsonPropertyName("call_id")]
    public string CallId { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("output")]
    public string Output { get; set; } = string.Empty;
}

