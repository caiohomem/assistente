using AssistenteExecutivo.Domain.Interfaces;

namespace AssistenteExecutivo.Domain.Entities;

public class AgentConfiguration
{
    private AgentConfiguration() { } // EF Core

    public AgentConfiguration(
        Guid configurationId,
        string ocrPrompt,
        IClock clock,
        string? transcriptionPrompt = null)
    {
        if (configurationId == Guid.Empty)
            throw new Domain.Exceptions.DomainException("Domain:ConfigurationIdObrigatorio");

        if (string.IsNullOrWhiteSpace(ocrPrompt))
            throw new Domain.Exceptions.DomainException("Domain:OcrPromptObrigatorio");

        if (clock == null)
            throw new Domain.Exceptions.DomainException("Domain:ClockObrigatorio");

        ConfigurationId = configurationId;
        OcrPrompt = ocrPrompt;
        TranscriptionPrompt = transcriptionPrompt;
        CreatedAt = clock.UtcNow;
        UpdatedAt = clock.UtcNow;
    }

    public Guid ConfigurationId { get; private set; }
    public string OcrPrompt { get; private set; } = null!;
    public string? TranscriptionPrompt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public static AgentConfiguration Create(
        Guid configurationId,
        string ocrPrompt,
        IClock clock,
        string? transcriptionPrompt = null)
    {
        return new AgentConfiguration(configurationId, ocrPrompt, clock, transcriptionPrompt);
    }

    public void UpdatePrompts(
        string ocrPrompt,
        IClock clock,
        string? transcriptionPrompt = null)
    {
        if (string.IsNullOrWhiteSpace(ocrPrompt))
            throw new Domain.Exceptions.DomainException("Domain:OcrPromptObrigatorio");

        if (clock == null)
            throw new Domain.Exceptions.DomainException("Domain:ClockObrigatorio");

        OcrPrompt = ocrPrompt;
        TranscriptionPrompt = transcriptionPrompt;
        UpdatedAt = clock.UtcNow;
    }
}





