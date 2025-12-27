using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Interfaces;
using MediatR;

namespace AssistenteExecutivo.Application.Commands.AgentConfiguration;

public class UpdateAgentConfigurationCommand : IRequest<AgentConfigurationDto>
{
    public string OcrPrompt { get; set; } = string.Empty;
    public string? TranscriptionPrompt { get; set; }
}

public class UpdateAgentConfigurationCommandHandler : IRequestHandler<UpdateAgentConfigurationCommand, AgentConfigurationDto>
{
    private readonly IAgentConfigurationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IIdGenerator _idGenerator;

    public UpdateAgentConfigurationCommandHandler(
        IAgentConfigurationRepository repository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IIdGenerator idGenerator)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _idGenerator = idGenerator;
    }

    public async Task<AgentConfigurationDto> Handle(UpdateAgentConfigurationCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetCurrentAsync(cancellationToken);
        
        if (existing != null)
        {
            // Atualizar configuração existente
            existing.UpdatePrompts(
                request.OcrPrompt,
                _clock,
                request.TranscriptionPrompt);
            await _repository.UpdateAsync(existing, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return new AgentConfigurationDto
            {
                ConfigurationId = existing.ConfigurationId,
                OcrPrompt = existing.OcrPrompt,
                TranscriptionPrompt = existing.TranscriptionPrompt,
                CreatedAt = existing.CreatedAt,
                UpdatedAt = existing.UpdatedAt
            };
        }
        else
        {
            // Criar nova configuração
            var configurationId = _idGenerator.NewGuid();
            var newConfiguration = Domain.Entities.AgentConfiguration.Create(
                configurationId,
                request.OcrPrompt,
                _clock,
                request.TranscriptionPrompt);
            
            await _repository.AddAsync(newConfiguration, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return new AgentConfigurationDto
            {
                ConfigurationId = newConfiguration.ConfigurationId,
                OcrPrompt = newConfiguration.OcrPrompt,
                TranscriptionPrompt = newConfiguration.TranscriptionPrompt,
                CreatedAt = newConfiguration.CreatedAt,
                UpdatedAt = newConfiguration.UpdatedAt
            };
        }
    }
}

