using MediatR;

namespace AssistenteExecutivo.Application.Commands.Assistant;

public class ProcessAssistantChatCommand : IRequest<ProcessAssistantChatResult>
{
    public Guid OwnerUserId { get; set; }
    public List<ChatMessage> Messages { get; set; } = new();
    public string? Model { get; set; }
}

public class ChatMessage
{
    public string Role { get; set; } = string.Empty; // "user", "assistant", "system"
    public string Content { get; set; } = string.Empty;
}

public class ProcessAssistantChatResult
{
    public string Message { get; set; } = string.Empty;
    public List<FunctionCallInfo> FunctionCalls { get; set; } = new();
}

public class FunctionCallInfo
{
    public string Name { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
}

