using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Interfaces;
using MediatR;

namespace AssistenteExecutivo.Application.Commands.AgentConfiguration;

public class UpdateAgentConfigurationCommand : IRequest<AgentConfigurationDto>
{
    public string ContextPrompt { get; set; } = string.Empty;
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
            existing.UpdateContextPrompt(request.ContextPrompt, _clock);
            await _repository.UpdateAsync(existing, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return new AgentConfigurationDto
            {
                ConfigurationId = existing.ConfigurationId,
                ContextPrompt = existing.ContextPrompt,
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
                request.ContextPrompt,
                _clock);
            
            await _repository.AddAsync(newConfiguration, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return new AgentConfigurationDto
            {
                ConfigurationId = newConfiguration.ConfigurationId,
                ContextPrompt = newConfiguration.ContextPrompt,
                CreatedAt = newConfiguration.CreatedAt,
                UpdatedAt = newConfiguration.UpdatedAt
            };
        }
    }
}

