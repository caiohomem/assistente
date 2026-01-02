using System.ComponentModel.DataAnnotations;

namespace AssistenteExecutivo.Application.DTOs;

public class AgentConfigurationDto
{
    public Guid ConfigurationId { get; set; }
    public string OcrPrompt { get; set; } = string.Empty;
    public string? TranscriptionPrompt { get; set; }
    public string? WorkflowPrompt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UpdateAgentConfigurationDto
{
    [Required]
    public string OcrPrompt { get; set; } = string.Empty;
    public string? TranscriptionPrompt { get; set; }
    public string? WorkflowPrompt { get; set; }
}
