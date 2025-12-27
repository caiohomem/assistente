using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Exceptions;
using MediatR;

namespace AssistenteExecutivo.Application.Queries.Notes;

public class ListNotesByContactQueryHandler : IRequestHandler<ListNotesByContactQuery, List<NoteDto>>
{
    private readonly INoteRepository _noteRepository;
    private readonly IContactRepository _contactRepository;

    public ListNotesByContactQueryHandler(
        INoteRepository noteRepository,
        IContactRepository contactRepository)
    {
        _noteRepository = noteRepository;
        _contactRepository = contactRepository;
    }

    public async Task<List<NoteDto>> Handle(ListNotesByContactQuery request, CancellationToken cancellationToken)
    {
        if (request.OwnerUserId == Guid.Empty)
            throw new DomainException("Domain:OwnerUserIdObrigatorio");

        // Validate that the contact belongs to the ownerUserId
        var contactExists = await _contactRepository.ExistsAsync(request.ContactId, request.OwnerUserId, cancellationToken);
        
        if (!contactExists)
            return new List<NoteDto>();

        // GetByContactIdAsync already validates ownerUserId through the join with Contacts
        var notes = await _noteRepository.GetByContactIdAsync(request.ContactId, request.OwnerUserId, cancellationToken);

        return notes.Select(MapToDTO).ToList();
    }

    private static NoteDto MapToDTO(Domain.Entities.Note note)
    {
        return new NoteDto
        {
            NoteId = note.NoteId,
            ContactId = note.ContactId,
            AuthorId = note.AuthorId,
            Type = note.Type,
            RawContent = note.RawContent,
            StructuredData = note.StructuredData,
            Version = note.Version,
            CreatedAt = note.CreatedAt,
            UpdatedAt = note.UpdatedAt
        };
    }
}






