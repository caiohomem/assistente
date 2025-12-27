using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AssistenteExecutivo.Infrastructure.Repositories;

public class AgentConfigurationRepository : IAgentConfigurationRepository
{
    private readonly ApplicationDbContext _context;

    public AgentConfigurationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AgentConfiguration?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        // Retorna a configuração mais recente (assumindo que só existe uma)
        return await _context.AgentConfigurations
            .OrderByDescending(c => c.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<AgentConfiguration?> GetByIdAsync(Guid configurationId, CancellationToken cancellationToken = default)
    {
        return await _context.AgentConfigurations
            .FirstOrDefaultAsync(c => c.ConfigurationId == configurationId, cancellationToken);
    }

    public async Task AddAsync(AgentConfiguration configuration, CancellationToken cancellationToken = default)
    {
        await _context.AgentConfigurations.AddAsync(configuration, cancellationToken);
    }

    public async Task UpdateAsync(AgentConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var entry = _context.Entry(configuration);
        
        if (entry.State == EntityState.Detached)
        {
            _context.AgentConfigurations.Update(configuration);
        }
        
        await Task.CompletedTask;
    }
}




