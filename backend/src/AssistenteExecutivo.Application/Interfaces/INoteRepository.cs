using AssistenteExecutivo.Domain.Entities;

namespace AssistenteExecutivo.Application.Interfaces;

public interface INoteRepository
{
    Task<Note?> GetByIdAsync(Guid noteId, CancellationToken cancellationToken = default);
    Task<List<Note>> GetByContactIdAsync(Guid contactId, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<List<Note>> GetByAuthorIdAsync(Guid authorId, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task AddAsync(Note note, CancellationToken cancellationToken = default);
    Task UpdateAsync(Note note, CancellationToken cancellationToken = default);
    Task DeleteAsync(Note note, CancellationToken cancellationToken = default);
}






