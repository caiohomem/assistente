using AssistenteExecutivo.Application.Commands.Assistant;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.Contacts;
using AssistenteExecutivo.Application.Commands.Contacts;
using AssistenteExecutivo.Application.Queries.Automation;
using AssistenteExecutivo.Application.Commands.Automation;
using AssistenteExecutivo.Application.Queries.Notes;
using AssistenteExecutivo.Application.Commands.Notes;
using AssistenteExecutivo.Domain.Enums;
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
            _logger.LogError("Erro ao chamar OpenAI API: {StatusCode}, {Error}. Payload enviado: {Payload}", response.StatusCode, errorContent, json);
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
                _ => throw new NotSupportedException($"Função não suportada: {functionName}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao executar função {FunctionName}. Payload: {Payload}", functionName, argumentsJson);
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
        
        // suggestedMessage é opcional, não precisa validar aqui

        var contactIdString = contactIdProp.GetString();
        if (string.IsNullOrWhiteSpace(contactIdString))
            throw new ArgumentException("contactId não pode ser vazio");

        if (!Guid.TryParse(contactIdString, out var contactId))
            throw new ArgumentException($"contactId inválido: '{contactIdString}'. Deve ser um GUID válido. Use a função search_contacts para encontrar o contato por nome primeiro.");

        var reason = reasonProp.GetString();
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("reason não pode ser vazio");

        var scheduledForString = scheduledForProp.GetString();
        if (string.IsNullOrWhiteSpace(scheduledForString))
            throw new ArgumentException("scheduledFor não pode ser vazio");

        if (!DateTime.TryParse(scheduledForString, out var scheduledFor))
            throw new ArgumentException($"scheduledFor inválido: '{scheduledForString}'. Deve estar no formato ISO 8601 (ex: 2024-12-25T10:00:00Z)");

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

    // ========== Funções de Contatos Adicionais ==========
    
    private async Task<object> ExecuteUpdateContactAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("id", out var idProp))
            throw new ArgumentException("id é obrigatório");

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
            throw new ArgumentException("id é obrigatório");

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
            throw new ArgumentException("contactId e email são obrigatórios");

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
            throw new ArgumentException("contactId e phone são obrigatórios");

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
            throw new ArgumentException("contactId e tag são obrigatórios");

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
            throw new ArgumentException("contactId, targetContactId e type são obrigatórios");

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
            throw new ArgumentException("relationshipId é obrigatório");

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

    // ========== Funções de Lembretes Adicionais ==========

    private async Task<object> ExecuteUpdateReminderStatusAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("id", out var idProp) || !args.TryGetProperty("newStatus", out var statusProp))
            throw new ArgumentException("id e newStatus são obrigatórios");

        if (!Enum.TryParse<ReminderStatus>(statusProp.GetString(), out var status))
            throw new ArgumentException("newStatus deve ser Pending, Sent, Dismissed ou Snoozed");

        var command = new UpdateReminderStatusCommand
        {
            ReminderId = Guid.Parse(idProp.GetString() ?? ""),
            OwnerUserId = ownerUserId,
            NewStatus = status,
            NewScheduledFor = args.TryGetProperty("newScheduledFor", out var dateProp) && dateProp.ValueKind != JsonValueKind.Null
                ? DateTime.Parse(dateProp.GetString() ?? "")
                : null
        };

        await _mediator.Send(command, cancellationToken);
        return new { success = true };
    }

    private async Task<object> ExecuteDeleteReminderAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("id", out var idProp))
            throw new ArgumentException("id é obrigatório");

        var command = new DeleteReminderCommand
        {
            ReminderId = Guid.Parse(idProp.GetString() ?? ""),
            OwnerUserId = ownerUserId
        };

        await _mediator.Send(command, cancellationToken);
        return new { success = true };
    }

    // ========== Funções de Notas ==========

    private async Task<object> ExecuteListContactNotesAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("contactId", out var contactIdProp))
            throw new ArgumentException("contactId é obrigatório");

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
            throw new ArgumentException("id é obrigatório");

        var query = new GetNoteByIdQuery
        {
            NoteId = Guid.Parse(idProp.GetString() ?? ""),
            OwnerUserId = ownerUserId
        };

        var result = await _mediator.Send(query, cancellationToken);
        if (result == null)
            throw new InvalidOperationException("Nota não encontrada");
        
        return result;
    }

    private async Task<object> ExecuteCreateTextNoteAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("contactId", out var contactIdProp) || !args.TryGetProperty("text", out var textProp))
            throw new ArgumentException("contactId e text são obrigatórios");

        var contactIdString = contactIdProp.GetString();
        if (string.IsNullOrWhiteSpace(contactIdString))
            throw new ArgumentException("contactId não pode ser vazio");

        if (!Guid.TryParse(contactIdString, out var contactId))
            throw new ArgumentException($"contactId inválido: '{contactIdString}'. Deve ser um GUID válido. Use a função search_contacts para encontrar o contato por nome primeiro.");

        var text = textProp.GetString();
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("text não pode ser vazio");

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
            throw new ArgumentException("id é obrigatório");

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
            throw new ArgumentException("id é obrigatório");

        var command = new DeleteNoteCommand
        {
            NoteId = Guid.Parse(idProp.GetString() ?? ""),
            OwnerUserId = ownerUserId
        };

        await _mediator.Send(command, cancellationToken);
        return new { success = true };
    }

    // ========== Funções de Drafts ==========

    private async Task<object> ExecuteCreateDraftAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("documentType", out var typeProp) || !args.TryGetProperty("content", out var contentProp))
            throw new ArgumentException("documentType e content são obrigatórios");

        if (!Enum.TryParse<DocumentType>(typeProp.GetString(), out var docType))
            throw new ArgumentException("documentType inválido");

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
            throw new ArgumentException("id é obrigatório");

        var query = new GetDraftByIdQuery
        {
            DraftId = Guid.Parse(idProp.GetString() ?? ""),
            OwnerUserId = ownerUserId
        };

        var result = await _mediator.Send(query, cancellationToken);
        if (result == null)
            throw new InvalidOperationException("Draft não encontrado");
        
        return result;
    }

    private async Task<object> ExecuteUpdateDraftAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("id", out var idProp))
            throw new ArgumentException("id é obrigatório");

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
            throw new ArgumentException("id é obrigatório");

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
            throw new ArgumentException("id é obrigatório");

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
            throw new ArgumentException("id é obrigatório");

        var command = new DeleteDraftCommand
        {
            DraftId = Guid.Parse(idProp.GetString() ?? ""),
            OwnerUserId = ownerUserId
        };

        await _mediator.Send(command, cancellationToken);
        return new { success = true };
    }

    // ========== Funções de Templates ==========

    private async Task<object> ExecuteCreateTemplateAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("name", out var nameProp) || !args.TryGetProperty("type", out var typeProp) || !args.TryGetProperty("body", out var bodyProp))
            throw new ArgumentException("name, type e body são obrigatórios");

        if (!Enum.TryParse<TemplateType>(typeProp.GetString(), out var templateType))
            throw new ArgumentException("type inválido");

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
            throw new ArgumentException("id é obrigatório");

        var query = new GetTemplateByIdQuery
        {
            TemplateId = Guid.Parse(idProp.GetString() ?? ""),
            OwnerUserId = ownerUserId
        };

        var result = await _mediator.Send(query, cancellationToken);
        if (result == null)
            throw new InvalidOperationException("Template não encontrado");
        
        return result;
    }

    private async Task<object> ExecuteUpdateTemplateAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("id", out var idProp))
            throw new ArgumentException("id é obrigatório");

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
            throw new ArgumentException("id é obrigatório");

        var command = new DeleteTemplateCommand
        {
            TemplateId = Guid.Parse(idProp.GetString() ?? ""),
            OwnerUserId = ownerUserId
        };

        await _mediator.Send(command, cancellationToken);
        return new { success = true };
    }

    // ========== Funções de Letterheads ==========

    private async Task<object> ExecuteCreateLetterheadAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("name", out var nameProp) || !args.TryGetProperty("designData", out var designProp))
            throw new ArgumentException("name e designData são obrigatórios");

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
            throw new ArgumentException("id é obrigatório");

        var query = new GetLetterheadByIdQuery
        {
            LetterheadId = Guid.Parse(idProp.GetString() ?? ""),
            OwnerUserId = ownerUserId
        };

        var result = await _mediator.Send(query, cancellationToken);
        if (result == null)
            throw new InvalidOperationException("Letterhead não encontrado");
        
        return result;
    }

    private async Task<object> ExecuteUpdateLetterheadAsync(Guid ownerUserId, JsonElement args, CancellationToken cancellationToken)
    {
        if (!args.TryGetProperty("id", out var idProp))
            throw new ArgumentException("id é obrigatório");

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
            throw new ArgumentException("id é obrigatório");

        var command = new DeleteLetterheadCommand
        {
            LetterheadId = Guid.Parse(idProp.GetString() ?? ""),
            OwnerUserId = ownerUserId
        };

        await _mediator.Send(command, cancellationToken);
        return new { success = true };
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
                    description = "Cria um novo lembrete para um contato. Útil para agendar follow-ups ou tarefas. IMPORTANTE: Se você não tiver o contactId (GUID) do contato, use primeiro a função search_contacts para encontrar o contato pelo nome e obter seu ID.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            contactId = new { type = "string", description = "ID do contato (GUID válido). Se você não tiver o GUID, use search_contacts primeiro para encontrar o contato pelo nome." },
                            reason = new { type = "string", description = "Motivo do lembrete (máximo 500 caracteres)" },
                            suggestedMessage = new { type = new[] { "string", "null" }, description = "Mensagem sugerida para o lembrete (máximo 2000 caracteres, opcional)" },
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
                            status = new { type = new[] { "string", "null" }, description = "Filtrar por status (Pending, Sent, Dismissed, Snoozed)", @enum = new[] { "Pending", "Sent", "Dismissed", "Snoozed" } },
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
            },
            // ========== Funções de Contatos Adicionais ==========
            new
            {
                type = "function",
                function = new
                {
                    name = "update_contact",
                    description = "Atualiza um contato existente. Todos os campos são opcionais, mas pelo menos um deve ser fornecido.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            id = new { type = "string", description = "ID do contato (GUID)" },
                            firstName = new { type = new[] { "string", "null" }, description = "Primeiro nome" },
                            lastName = new { type = new[] { "string", "null" }, description = "Sobrenome" },
                            jobTitle = new { type = new[] { "string", "null" }, description = "Cargo" },
                            company = new { type = new[] { "string", "null" }, description = "Empresa" },
                            street = new { type = new[] { "string", "null" }, description = "Endereço (rua)" },
                            city = new { type = new[] { "string", "null" }, description = "Cidade" },
                            state = new { type = new[] { "string", "null" }, description = "Estado" },
                            zipCode = new { type = new[] { "string", "null" }, description = "CEP" },
                            country = new { type = new[] { "string", "null" }, description = "País" }
                        },
                        required = new[] { "id", "firstName", "lastName", "jobTitle", "company", "street", "city", "state", "zipCode", "country" },
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
                    name = "delete_contact",
                    description = "Deleta um contato (soft delete)",
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
                    name = "add_contact_email",
                    description = "Adiciona um email a um contato",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            contactId = new { type = "string", description = "ID do contato (GUID)" },
                            email = new { type = "string", description = "Endereço de email" }
                        },
                        required = new[] { "contactId", "email" },
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
                    name = "add_contact_phone",
                    description = "Adiciona um telefone a um contato",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            contactId = new { type = "string", description = "ID do contato (GUID)" },
                            phone = new { type = "string", description = "Número de telefone" }
                        },
                        required = new[] { "contactId", "phone" },
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
                    name = "add_contact_tag",
                    description = "Adiciona uma tag a um contato",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            contactId = new { type = "string", description = "ID do contato (GUID)" },
                            tag = new { type = "string", description = "Nome da tag" }
                        },
                        required = new[] { "contactId", "tag" },
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
                    name = "add_contact_relationship",
                    description = "Adiciona um relacionamento entre contatos",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            contactId = new { type = "string", description = "ID do contato de origem (GUID)" },
                            targetContactId = new { type = "string", description = "ID do contato de destino (GUID)" },
                            type = new { type = "string", description = "Tipo de relacionamento (ex: \"colleague\", \"friend\", \"family\")" },
                            description = new { type = new[] { "string", "null" }, description = "Descrição do relacionamento" },
                            strength = new { type = new[] { "number", "null" }, description = "Força do relacionamento (0-1)" },
                            isConfirmed = new { type = new[] { "boolean", "null" }, description = "Se o relacionamento é confirmado" }
                        },
                        required = new[] { "contactId", "targetContactId", "type", "description", "strength", "isConfirmed" },
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
                    name = "delete_contact_relationship",
                    description = "Deleta um relacionamento entre contatos",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            relationshipId = new { type = "string", description = "ID do relacionamento (GUID)" }
                        },
                        required = new[] { "relationshipId" },
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
                    name = "get_network_graph",
                    description = "Obtém o grafo de relacionamentos entre contatos",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            maxDepth = new { type = new[] { "number", "null" }, description = "Profundidade máxima do grafo (padrão: 2)" }
                        },
                        required = new[] { "maxDepth" },
                        additionalProperties = false
                    },
                    strict = true
                }
            },
            // ========== Funções de Lembretes Adicionais ==========
            new
            {
                type = "function",
                function = new
                {
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
                }
            },
            new
            {
                type = "function",
                function = new
                {
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
                }
            },
            // ========== Funções de Notas ==========
            new
            {
                type = "function",
                function = new
                {
                    name = "list_contact_notes",
                    description = "Lista todas as notas de um contato específico",
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
                }
            },
            new
            {
                type = "function",
                function = new
                {
                    name = "get_note",
                    description = "Obtém uma nota específica por ID",
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
                }
            },
            new
            {
                type = "function",
                function = new
                {
                    name = "create_text_note",
                    description = "Cria uma nova nota de texto para um contato. IMPORTANTE: Se você não tiver o contactId (GUID) do contato, use primeiro a função search_contacts para encontrar o contato pelo nome e obter seu ID.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            contactId = new { type = "string", description = "ID do contato (GUID). Se você não tiver o GUID, use search_contacts primeiro para encontrar o contato pelo nome." },
                            text = new { type = "string", description = "Conteúdo da nota" },
                            structuredData = new { type = new[] { "string", "null" }, description = "Dados estruturados em JSON (opcional)" }
                        },
                        required = new[] { "contactId", "text", "structuredData" },
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
                    name = "update_note",
                    description = "Atualiza uma nota existente",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            id = new { type = "string", description = "ID da nota (GUID)" },
                            rawContent = new { type = new[] { "string", "null" }, description = "Conteúdo bruto da nota" },
                            structuredData = new { type = new[] { "string", "null" }, description = "Dados estruturados em JSON" }
                        },
                        required = new[] { "id", "rawContent", "structuredData" },
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
                }
            },
            // ========== Funções de Drafts ==========
            new
            {
                type = "function",
                function = new
                {
                    name = "create_draft",
                    description = "Cria um novo draft de documento",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            documentType = new { type = "string", description = "Tipo de documento", @enum = new[] { "Email", "Oficio", "Invite" } },
                            content = new { type = "string", description = "Conteúdo do documento" },
                            contactId = new { type = new[] { "string", "null" }, description = "ID do contato relacionado (GUID, opcional)" },
                            companyId = new { type = new[] { "string", "null" }, description = "ID da empresa relacionada (GUID, opcional)" },
                            templateId = new { type = new[] { "string", "null" }, description = "ID do template usado (GUID, opcional)" },
                            letterheadId = new { type = new[] { "string", "null" }, description = "ID do papel timbrado usado (GUID, opcional)" }
                        },
                        required = new[] { "documentType", "content", "contactId", "companyId", "templateId", "letterheadId" },
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
                    name = "list_drafts",
                    description = "Lista drafts do usuário autenticado",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            contactId = new { type = new[] { "string", "null" }, description = "Filtrar por ID do contato (GUID)" },
                            companyId = new { type = new[] { "string", "null" }, description = "Filtrar por ID da empresa (GUID)" },
                            documentType = new { type = new[] { "string", "null" }, description = "Filtrar por tipo de documento", @enum = new[] { "Email", "Oficio", "Invite" } },
                            status = new { type = new[] { "string", "null" }, description = "Filtrar por status", @enum = new[] { "Draft", "Approved", "Sent" } },
                            page = new { type = new[] { "number", "null" }, description = "Número da página (padrão: 1)" },
                            pageSize = new { type = new[] { "number", "null" }, description = "Tamanho da página (padrão: 20)" }
                        },
                        required = new[] { "contactId", "companyId", "documentType", "status", "page", "pageSize" },
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
                    name = "get_draft",
                    description = "Obtém um draft específico por ID",
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
                }
            },
            new
            {
                type = "function",
                function = new
                {
                    name = "update_draft",
                    description = "Atualiza um draft",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            id = new { type = "string", description = "ID do draft (GUID)" },
                            content = new { type = new[] { "string", "null" }, description = "Conteúdo do documento" },
                            contactId = new { type = new[] { "string", "null" }, description = "ID do contato relacionado (GUID)" },
                            companyId = new { type = new[] { "string", "null" }, description = "ID da empresa relacionada (GUID)" },
                            templateId = new { type = new[] { "string", "null" }, description = "ID do template usado (GUID)" },
                            letterheadId = new { type = new[] { "string", "null" }, description = "ID do papel timbrado usado (GUID)" }
                        },
                        required = new[] { "id", "content", "contactId", "companyId", "templateId", "letterheadId" },
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
                }
            },
            new
            {
                type = "function",
                function = new
                {
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
                }
            },
            new
            {
                type = "function",
                function = new
                {
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
                }
            },
            // ========== Funções de Templates ==========
            new
            {
                type = "function",
                function = new
                {
                    name = "create_template",
                    description = "Cria um novo template de documento",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            name = new { type = "string", description = "Nome do template (máximo 200 caracteres)" },
                            type = new { type = "string", description = "Tipo de template", @enum = new[] { "Email", "Oficio", "Invite", "Generic" } },
                            body = new { type = "string", description = "Corpo do template" },
                            placeholdersSchema = new { type = new[] { "string", "null" }, description = "Schema JSON dos placeholders (opcional)" }
                        },
                        required = new[] { "name", "type", "body", "placeholdersSchema" },
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
                    name = "list_templates",
                    description = "Lista templates do usuário autenticado",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            type = new { type = new[] { "string", "null" }, description = "Filtrar por tipo", @enum = new[] { "Email", "Oficio", "Invite", "Generic" } },
                            activeOnly = new { type = new[] { "boolean", "null" }, description = "Apenas templates ativos (padrão: false)" },
                            page = new { type = new[] { "number", "null" }, description = "Número da página (padrão: 1)" },
                            pageSize = new { type = new[] { "number", "null" }, description = "Tamanho da página (padrão: 20)" }
                        },
                        required = new[] { "type", "activeOnly", "page", "pageSize" },
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
                    name = "get_template",
                    description = "Obtém um template específico por ID",
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
                }
            },
            new
            {
                type = "function",
                function = new
                {
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
                            active = new { type = new[] { "boolean", "null" }, description = "Se o template está ativo" }
                        },
                        required = new[] { "id", "name", "body", "placeholdersSchema", "active" },
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
                }
            },
            // ========== Funções de Letterheads ==========
            new
            {
                type = "function",
                function = new
                {
                    name = "create_letterhead",
                    description = "Cria um novo papel timbrado",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            name = new { type = "string", description = "Nome do papel timbrado (máximo 200 caracteres)" },
                            designData = new { type = "string", description = "Dados de design em JSON" }
                        },
                        required = new[] { "name", "designData" },
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
                    name = "list_letterheads",
                    description = "Lista papéis timbrados do usuário autenticado",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            activeOnly = new { type = new[] { "boolean", "null" }, description = "Apenas papéis timbrados ativos (padrão: false)" },
                            page = new { type = new[] { "number", "null" }, description = "Número da página (padrão: 1)" },
                            pageSize = new { type = new[] { "number", "null" }, description = "Tamanho da página (padrão: 20)" }
                        },
                        required = new[] { "activeOnly", "page", "pageSize" },
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
                    name = "get_letterhead",
                    description = "Obtém um papel timbrado específico por ID",
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
                }
            },
            new
            {
                type = "function",
                function = new
                {
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
                            isActive = new { type = new[] { "boolean", "null" }, description = "Se o papel timbrado está ativo" }
                        },
                        required = new[] { "id", "name", "designData", "isActive" },
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

