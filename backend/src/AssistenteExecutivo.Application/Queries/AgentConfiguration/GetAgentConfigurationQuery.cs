using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Interfaces;
using MediatR;

namespace AssistenteExecutivo.Application.Queries.AgentConfiguration;

public class GetAgentConfigurationQuery : IRequest<AgentConfigurationDto?>
{
}

public class GetAgentConfigurationQueryHandler : IRequestHandler<GetAgentConfigurationQuery, AgentConfigurationDto?>
{
    private readonly IAgentConfigurationRepository _repository;

    public GetAgentConfigurationQueryHandler(IAgentConfigurationRepository repository)
    {
        _repository = repository;
    }

    public async Task<AgentConfigurationDto?> Handle(GetAgentConfigurationQuery request, CancellationToken cancellationToken)
    {
        var configuration = await _repository.GetCurrentAsync(cancellationToken);
        
        if (configuration == null)
        {
            return null;
        }

        return new AgentConfigurationDto
        {
            ConfigurationId = configuration.ConfigurationId,
            ContextPrompt = configuration.ContextPrompt,
            CreatedAt = configuration.CreatedAt,
            UpdatedAt = configuration.UpdatedAt
        };
    }
}

