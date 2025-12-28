using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AssistenteExecutivo.Infrastructure.Repositories;

public class ContactRepository : IContactRepository
{
    private readonly ApplicationDbContext _context;

    public ContactRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Contact?> GetByIdAsync(Guid contactId, Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.Contacts
            .Include(c => c.Emails)
            .Include(c => c.Phones)
            .Include(c => c.Tags)
            .Include(c => c.Relationships)
            .FirstOrDefaultAsync(c => c.ContactId == contactId && c.OwnerUserId == ownerUserId && !c.IsDeleted, cancellationToken);
    }

    public async Task<List<Contact>> GetAllAsync(Guid ownerUserId, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Contacts
    .Include(c => c.Emails)
    .Include(c => c.Phones)
    .Include(c => c.Tags)
    .Include(c => c.Relationships)
    .Where(c => c.OwnerUserId == ownerUserId);

        if (!includeDeleted)
        {
            query = query.Where(c => !c.IsDeleted);
        }

        var contacts = await query.ToListAsync(cancellationToken);
        return contacts;
    }

    public async Task<Contact?> GetByEmailAsync(string email, Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        // Normalize email (same as EmailAddress does)
        var normalizedEmail = email.Trim().ToLowerInvariant();

        return await _context.Contacts
            .Include(c => c.Emails)
            .Include(c => c.Phones)
            .Include(c => c.Tags)
            .Include(c => c.Relationships)
            .Where(c => c.OwnerUserId == ownerUserId && !c.IsDeleted)
            .Where(c => c.Emails.Any(e => e.Value == normalizedEmail))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Contact?> GetByPhoneAsync(string phone, Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return null;

        // Remove formatting from phone (same as PhoneNumber does)
        var cleanNumber = System.Text.RegularExpressions.Regex.Replace(phone, @"[^\d]", "");

        return await _context.Contacts
            .Include(c => c.Emails)
            .Include(c => c.Phones)
            .Include(c => c.Tags)
            .Include(c => c.Relationships)
            .Where(c => c.OwnerUserId == ownerUserId && !c.IsDeleted)
            .Where(c => c.Phones.Any(p => p.Number == cleanNumber))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(Contact contact, CancellationToken cancellationToken = default)
    {
        await _context.Contacts.AddAsync(contact, cancellationToken);
    }

    public async Task UpdateAsync(Contact contact, CancellationToken cancellationToken = default)
    {
        var entry = _context.Entry(contact);

        // Se a entidade não está sendo rastreada, adiciona ao contexto
        if (entry.State == EntityState.Detached)
        {
            _context.Contacts.Update(contact);
        }
        // Se a entidade já está sendo rastreada, o EF Core detectará automaticamente:
        // - Mudanças em propriedades (como UpdatedAt quando setter é chamado)
        // - Novas entidades em coleções de navegação (como Relationships adicionadas)
        // Não precisamos fazer nada adicional - o change tracker já está monitorando

        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Contact contact, CancellationToken cancellationToken = default)
    {
        // Soft delete: marca como deletado
        contact.Delete();
        _context.Contacts.Update(contact);
        await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Guid contactId, Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.Contacts
            .AnyAsync(c => c.ContactId == contactId && c.OwnerUserId == ownerUserId && !c.IsDeleted, cancellationToken);
    }

    public async Task<(bool Exists, Guid? OwnerUserId, bool IsDeleted)> GetContactStatusAsync(Guid contactId, CancellationToken cancellationToken = default)
    {
        var contact = await _context.Contacts
            .Where(c => c.ContactId == contactId)
            .Select(c => new { c.OwnerUserId, c.IsDeleted })
            .FirstOrDefaultAsync(cancellationToken);

        if (contact == null)
            return (false, null, false);

        return (true, contact.OwnerUserId, contact.IsDeleted);
    }

    public async Task<NetworkGraphDto> GetNetworkGraphAsync(Guid ownerUserId, int maxDepth, CancellationToken cancellationToken = default)
    {
        // Buscar todos os contatos do usuário (nós do grafo)
        var contacts = await _context.Contacts
            .Where(c => c.OwnerUserId == ownerUserId && !c.IsDeleted)
            .Include(c => c.Emails)
            .ToListAsync(cancellationToken);

        var allContacts = contacts.Select(c => new
        {
            c.ContactId,
            FirstName = c.Name.FirstName,
            LastName = c.Name.LastName,
            c.JobTitle,
            c.Company,
            PrimaryEmail = c.Emails.OrderBy(e => e.Value).FirstOrDefault()?.Value
        }).ToList();

        // Buscar todos os relacionamentos entre os contatos do usuário
        var contactIds = allContacts.Select(c => c.ContactId).ToList();
        
        var relationships = await _context.Relationships
            .Where(r => contactIds.Contains(r.SourceContactId) && contactIds.Contains(r.TargetContactId))
            .Select(r => new
            {
                r.RelationshipId,
                r.SourceContactId,
                r.TargetContactId,
                r.Type,
                r.Description,
                r.Strength,
                r.IsConfirmed
            })
            .ToListAsync(cancellationToken);

        // Se maxDepth > 1, buscar relacionamentos indiretos (através de outros contatos)
        // Por enquanto, vamos retornar todos os contatos e relacionamentos diretos
        // Uma otimização futura seria usar CTE recursiva para buscar até maxDepth níveis

        // Mapear para DTOs
        var nodes = allContacts.Select(c => new GraphNodeDto
        {
            ContactId = c.ContactId,
            FullName = $"{c.FirstName} {c.LastName}".Trim(),
            Company = c.Company,
            JobTitle = c.JobTitle,
            PrimaryEmail = c.PrimaryEmail
        }).ToList();

        var edges = relationships.Select(r => new GraphEdgeDto
        {
            RelationshipId = r.RelationshipId,
            SourceContactId = r.SourceContactId,
            TargetContactId = r.TargetContactId,
            Type = r.Type,
            Description = r.Description,
            Strength = r.Strength,
            IsConfirmed = r.IsConfirmed
        }).ToList();

        return new NetworkGraphDto
        {
            Nodes = nodes,
            Edges = edges
        };
    }
}

