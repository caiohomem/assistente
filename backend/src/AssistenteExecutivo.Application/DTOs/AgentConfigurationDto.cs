using System.ComponentModel.DataAnnotations;

namespace AssistenteExecutivo.Application.DTOs;

public class AgentConfigurationDto
{
    public Guid ConfigurationId { get; set; }
    public string ContextPrompt { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UpdateAgentConfigurationDto
{
    [Required]
    public string ContextPrompt { get; set; } = string.Empty;
}

