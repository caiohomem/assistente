using AssistenteExecutivo.Application.Commands.Notes;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Notes;

public class CreateAudioNoteCommandHandler : IRequestHandler<CreateAudioNoteCommand, Guid>
{
    private readonly INoteRepository _noteRepository;
    private readonly IContactRepository _contactRepository;
    private readonly IClock _clock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;

    public CreateAudioNoteCommandHandler(
        INoteRepository noteRepository,
        IContactRepository contactRepository,
        IClock clock,
        IUnitOfWork unitOfWork,
        IPublisher publisher)
    {
        _noteRepository = noteRepository;
        _contactRepository = contactRepository;
        _clock = clock;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task<Guid> Handle(CreateAudioNoteCommand request, CancellationToken cancellationToken)
    {
        var (exists, ownerUserId, isDeleted) = await _contactRepository.GetContactStatusAsync(request.ContactId, cancellationToken);
        if (!exists || isDeleted)
            throw new DomainException("Domain:ContactIdObrigatorio");

        if (ownerUserId != request.AuthorId)
            throw new DomainException("Domain:AuthorIdInvalido");

        // Criar nota de áudio
        var noteId = Guid.NewGuid();
        var note = Note.CreateAudioNote(
            noteId,
            request.ContactId,
            request.AuthorId,
            request.Transcript,
            _clock);

        // Adicionar structured data se fornecido
        if (!string.IsNullOrWhiteSpace(request.StructuredData))
        {
            note.UpdateStructuredData(request.StructuredData, _clock);
        }

        // Adicionar ao repositório
        await _noteRepository.AddAsync(note, cancellationToken);

        // Salvar alterações
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publicar eventos de domínio
        foreach (var domainEvent in note.DomainEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }
        note.ClearDomainEvents();

        return note.NoteId;
    }
}
