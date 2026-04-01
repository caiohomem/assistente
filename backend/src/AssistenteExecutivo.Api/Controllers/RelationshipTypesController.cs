using AssistenteExecutivo.Api.Auth;
using AssistenteExecutivo.Api.Extensions;
using AssistenteExecutivo.Application.Commands.RelationshipTypes;
using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Queries.RelationshipTypes;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace AssistenteExecutivo.Api.Controllers;

[ApiController]
[Route("api/relationship-types")]
[Authorize(AuthenticationSchemes = $"{BffSessionAuthenticationDefaults.Scheme},{JwtBearerDefaults.AuthenticationScheme}")]
public sealed class RelationshipTypesController : ControllerBase
{
    private readonly IMediator _mediator;

    public RelationshipTypesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<RelationshipTypeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListRelationshipTypes(CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var query = new ListRelationshipTypesQuery
        {
            OwnerUserId = ownerUserId
        };

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(RelationshipTypeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRelationshipType(
        [FromBody] UpsertRelationshipTypeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new CreateRelationshipTypeCommand
        {
            OwnerUserId = ownerUserId,
            Name = request.Name
        };

        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(ListRelationshipTypes), new { id = result.RelationshipTypeId }, result);
    }

    [HttpPut("{relationshipTypeId:guid}")]
    [ProducesResponseType(typeof(RelationshipTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateRelationshipType(
        [FromRoute] Guid relationshipTypeId,
        [FromBody] UpsertRelationshipTypeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new UpdateRelationshipTypeCommand
        {
            OwnerUserId = ownerUserId,
            RelationshipTypeId = relationshipTypeId,
            Name = request.Name
        };

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{relationshipTypeId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteRelationshipType(
        [FromRoute] Guid relationshipTypeId,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new DeleteRelationshipTypeCommand
        {
            OwnerUserId = ownerUserId,
            RelationshipTypeId = relationshipTypeId
        };

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    public sealed record UpsertRelationshipTypeRequest([Required] string Name);
}
