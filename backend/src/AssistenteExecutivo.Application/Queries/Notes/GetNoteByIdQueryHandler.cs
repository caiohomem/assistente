using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Exceptions;
using MediatR;

namespace AssistenteExecutivo.Application.Queries.Notes;

public class GetNoteByIdQueryHandler : IRequestHandler<GetNoteByIdQuery, NoteDto?>
{
    private readonly INoteRepository _noteRepository;
    private readonly IContactRepository _contactRepository;

    public GetNoteByIdQueryHandler(
        INoteRepository noteRepository,
        IContactRepository contactRepository)
    {
        _noteRepository = noteRepository;
        _contactRepository = contactRepository;
    }

    public async Task<NoteDto?> Handle(GetNoteByIdQuery request, CancellationToken cancellationToken)
    {
        if (request.OwnerUserId == Guid.Empty)
            throw new DomainException("Domain:OwnerUserIdObrigatorio");

        var note = await _noteRepository.GetByIdAsync(request.NoteId, cancellationToken);

        if (note == null)
            return null;

        // Validate that the contact belongs to the ownerUserId
        var contactExists = await _contactRepository.ExistsAsync(note.ContactId, request.OwnerUserId, cancellationToken);
        
        if (!contactExists)
            return null;

        return MapToDTO(note);
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





