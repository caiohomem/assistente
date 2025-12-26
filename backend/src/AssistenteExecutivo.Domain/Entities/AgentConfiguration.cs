using AssistenteExecutivo.Domain.Interfaces;

namespace AssistenteExecutivo.Domain.Entities;

public class AgentConfiguration
{
    private AgentConfiguration() { } // EF Core

    public AgentConfiguration(
        Guid configurationId,
        string contextPrompt,
        IClock clock)
    {
        if (configurationId == Guid.Empty)
            throw new Domain.Exceptions.DomainException("Domain:ConfigurationIdObrigatorio");

        if (string.IsNullOrWhiteSpace(contextPrompt))
            throw new Domain.Exceptions.DomainException("Domain:ContextPromptObrigatorio");

        if (clock == null)
            throw new Domain.Exceptions.DomainException("Domain:ClockObrigatorio");

        ConfigurationId = configurationId;
        ContextPrompt = contextPrompt;
        CreatedAt = clock.UtcNow;
        UpdatedAt = clock.UtcNow;
    }

    public Guid ConfigurationId { get; private set; }
    public string ContextPrompt { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public static AgentConfiguration Create(
        Guid configurationId,
        string contextPrompt,
        IClock clock)
    {
        return new AgentConfiguration(configurationId, contextPrompt, clock);
    }

    public void UpdateContextPrompt(string contextPrompt, IClock clock)
    {
        if (string.IsNullOrWhiteSpace(contextPrompt))
            throw new Domain.Exceptions.DomainException("Domain:ContextPromptObrigatorio");

        if (clock == null)
            throw new Domain.Exceptions.DomainException("Domain:ClockObrigatorio");

        ContextPrompt = contextPrompt;
        UpdatedAt = clock.UtcNow;
    }
}


