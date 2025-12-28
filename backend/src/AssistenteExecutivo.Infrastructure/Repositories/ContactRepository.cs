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
}

