using AssistenteExecutivo.Api.Auth;
using AssistenteExecutivo.Api.Extensions;
using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssistenteExecutivo.Api.Controllers;

[ApiController]
[Route("api/agent-configuration")]
[Authorize(AuthenticationSchemes = $"{BffSessionAuthenticationDefaults.Scheme},{JwtBearerDefaults.AuthenticationScheme}")]
public sealed class AgentConfigurationController : ControllerBase
{
    private readonly IAgentConfigurationRepository _repository;
    private readonly IClock _clock;
    private readonly IIdGenerator _idGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public AgentConfigurationController(
        IAgentConfigurationRepository repository,
        IClock clock,
        IIdGenerator idGenerator,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _clock = clock;
        _idGenerator = idGenerator;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Obtém a configuração atual do agente.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(AgentConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrent(CancellationToken cancellationToken = default)
    {
        var configuration = await _repository.GetCurrentAsync(cancellationToken);
        
        if (configuration == null)
        {
            return NotFound(new { message = "Configuração do agente não encontrada" });
        }

        var dto = new AgentConfigurationDto
        {
            ConfigurationId = configuration.ConfigurationId,
            ContextPrompt = configuration.ContextPrompt,
            CreatedAt = configuration.CreatedAt,
            UpdatedAt = configuration.UpdatedAt
        };

        return Ok(dto);
    }

    /// <summary>
    /// Atualiza ou cria a configuração do agente.
    /// </summary>
    [HttpPut]
    [ProducesResponseType(typeof(AgentConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AgentConfigurationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateOrCreate(
        [FromBody] UpdateAgentConfigurationDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var existing = await _repository.GetCurrentAsync(cancellationToken);
        
        if (existing != null)
        {
            // Atualizar configuração existente
            existing.UpdateContextPrompt(dto.ContextPrompt, _clock);
            await _repository.UpdateAsync(existing, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            var responseDto = new AgentConfigurationDto
            {
                ConfigurationId = existing.ConfigurationId,
                ContextPrompt = existing.ContextPrompt,
                CreatedAt = existing.CreatedAt,
                UpdatedAt = existing.UpdatedAt
            };
            
            return Ok(responseDto);
        }
        else
        {
            // Criar nova configuração
            var configurationId = _idGenerator.NewGuid();
            var newConfiguration = AgentConfiguration.Create(
                configurationId,
                dto.ContextPrompt,
                _clock);
            
            await _repository.AddAsync(newConfiguration, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            var responseDto = new AgentConfigurationDto
            {
                ConfigurationId = newConfiguration.ConfigurationId,
                ContextPrompt = newConfiguration.ContextPrompt,
                CreatedAt = newConfiguration.CreatedAt,
                UpdatedAt = newConfiguration.UpdatedAt
            };
            
            return CreatedAtAction(nameof(GetCurrent), responseDto);
        }
    }
}

