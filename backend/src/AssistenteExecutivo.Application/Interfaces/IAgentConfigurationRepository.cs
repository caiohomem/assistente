using AssistenteExecutivo.Domain.Entities;

namespace AssistenteExecutivo.Application.Interfaces;

public interface IAgentConfigurationRepository
{
    Task<AgentConfiguration?> GetCurrentAsync(CancellationToken cancellationToken = default);
    Task<AgentConfiguration?> GetByIdAsync(Guid configurationId, CancellationToken cancellationToken = default);
    Task AddAsync(AgentConfiguration configuration, CancellationToken cancellationToken = default);
    Task UpdateAsync(AgentConfiguration configuration, CancellationToken cancellationToken = default);
}


