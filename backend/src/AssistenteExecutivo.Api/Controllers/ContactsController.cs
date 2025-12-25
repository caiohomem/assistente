using System.ComponentModel.DataAnnotations;
using AssistenteExecutivo.Api.Auth;
using AssistenteExecutivo.Api.Extensions;
using AssistenteExecutivo.Application.Commands.Contacts;
using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Queries.Contacts;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssistenteExecutivo.Api.Controllers;

[ApiController]
[Route("api/contacts")]
[Authorize(AuthenticationSchemes = $"{BffSessionAuthenticationDefaults.Scheme},{JwtBearerDefaults.AuthenticationScheme}")]
public sealed class ContactsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ContactsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Lista todos os contatos do usuário autenticado.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ListContactsResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListContacts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var query = new ListContactsQuery
        {
            OwnerUserId = ownerUserId,
            Page = page,
            PageSize = pageSize,
            IncludeDeleted = includeDeleted
        };

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Busca contatos do usuário autenticado com filtro de texto.
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(SearchContactsResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SearchContacts(
        [FromQuery] string? searchTerm = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var query = new SearchContactsQuery
        {
            OwnerUserId = ownerUserId,
            SearchTerm = searchTerm,
            Page = page,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Obtém um contato específico por ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ContactDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetContactById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var query = new GetContactByIdQuery
        {
            ContactId = id,
            OwnerUserId = ownerUserId
        };

        var result = await _mediator.Send(query, cancellationToken);
        if (result == null)
            return NotFound(new { message = "Contato não encontrado." });

        return Ok(result);
    }

    /// <summary>
    /// Cria um novo contato.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateContact(
        [FromBody] CreateContactRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new CreateContactCommand
        {
            OwnerUserId = ownerUserId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            JobTitle = request.JobTitle,
            Company = request.Company,
            Street = request.Street,
            City = request.City,
            State = request.State,
            ZipCode = request.ZipCode,
            Country = request.Country
        };

        var contactId = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetContactById), new { id = contactId }, new { contactId });
    }

    /// <summary>
    /// Atualiza um contato existente.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateContact(
        [FromRoute] Guid id,
        [FromBody] UpdateContactRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new UpdateContactCommand
        {
            ContactId = id,
            OwnerUserId = ownerUserId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            JobTitle = request.JobTitle,
            Company = request.Company,
            Street = request.Street,
            City = request.City,
            State = request.State,
            ZipCode = request.ZipCode,
            Country = request.Country
        };

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Deleta um contato (soft delete).
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteContact(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new DeleteContactCommand
        {
            ContactId = id,
            OwnerUserId = ownerUserId
        };

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Adiciona um email a um contato.
    /// </summary>
    [HttpPost("{id}/emails")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddEmail(
        [FromRoute] Guid id,
        [FromBody] AddEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new AddContactEmailCommand
        {
            ContactId = id,
            OwnerUserId = ownerUserId,
            Email = request.Email
        };

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Adiciona um telefone a um contato.
    /// </summary>
    [HttpPost("{id}/phones")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddPhone(
        [FromRoute] Guid id,
        [FromBody] AddPhoneRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new AddContactPhoneCommand
        {
            ContactId = id,
            OwnerUserId = ownerUserId,
            Phone = request.Phone
        };

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Adiciona uma tag a um contato.
    /// </summary>
    [HttpPost("{id}/tags")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddTag(
        [FromRoute] Guid id,
        [FromBody] AddTagRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new AddContactTagCommand
        {
            ContactId = id,
            OwnerUserId = ownerUserId,
            Tag = request.Tag
        };

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Adiciona um relacionamento entre contatos.
    /// </summary>
    [HttpPost("{id}/relationships")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddRelationship(
        [FromRoute] Guid id,
        [FromBody] AddRelationshipRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Validar que o ID da rota (sourceContactId) não está vazio
        if (id == Guid.Empty)
            return BadRequest(new { message = "ID do contato de origem é obrigatório." });

        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new AddContactRelationshipCommand
        {
            ContactId = id, // SourceContactId vem da rota
            OwnerUserId = ownerUserId,
            TargetContactId = request.TargetContactId,
            Type = request.Type,
            Description = request.Description
        };

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Deleta um relacionamento.
    /// </summary>
    [HttpDelete("relationships/{relationshipId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRelationship(
        [FromRoute] Guid relationshipId,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await HttpContext.GetRequiredOwnerUserIdAsync(_mediator, cancellationToken);

        var command = new DeleteRelationshipCommand
        {
            RelationshipId = relationshipId,
            OwnerUserId = ownerUserId
        };

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    // Request DTOs
    public sealed record CreateContactRequest(
        [Required] string FirstName,
        string? LastName,
        string? JobTitle,
        string? Company,
        string? Street,
        string? City,
        string? State,
        string? ZipCode,
        string? Country);

    public sealed record UpdateContactRequest(
        string? FirstName,
        string? LastName,
        string? JobTitle,
        string? Company,
        string? Street,
        string? City,
        string? State,
        string? ZipCode,
        string? Country);

    public sealed record AddEmailRequest([Required] string Email);
    public sealed record AddPhoneRequest([Required] string Phone);
    public sealed record AddTagRequest([Required] string Tag);

    public sealed record AddRelationshipRequest(
        [Required] Guid TargetContactId,
        [Required] string Type,
        string? Description);
}

