using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AssistenteExecutivo.Infrastructure.Repositories;

public class NoteRepository : INoteRepository
{
    private readonly ApplicationDbContext _context;

    public NoteRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Note?> GetByIdAsync(Guid noteId, CancellationToken cancellationToken = default)
    {
        return await _context.Notes
            .FirstOrDefaultAsync(n => n.NoteId == noteId, cancellationToken);
    }

    public async Task<List<Note>> GetByContactIdAsync(Guid contactId, Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.Notes
            .Join(_context.Contacts,
                note => note.ContactId,
                contact => contact.ContactId,
                (note, contact) => new { Note = note, Contact = contact })
            .Where(x => x.Note.ContactId == contactId && x.Contact.OwnerUserId == ownerUserId)
            .Select(x => x.Note)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Note>> GetByAuthorIdAsync(Guid authorId, Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.Notes
            .Join(_context.Contacts,
                note => note.ContactId,
                contact => contact.ContactId,
                (note, contact) => new { Note = note, Contact = contact })
            .Where(x => x.Note.AuthorId == authorId && x.Contact.OwnerUserId == ownerUserId)
            .Select(x => x.Note)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Note note, CancellationToken cancellationToken = default)
    {
        await _context.Notes.AddAsync(note, cancellationToken);
    }

    public async Task UpdateAsync(Note note, CancellationToken cancellationToken = default)
    {
        _context.Notes.Update(note);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Note note, CancellationToken cancellationToken = default)
    {
        _context.Notes.Remove(note);
        await Task.CompletedTask;
    }
}

