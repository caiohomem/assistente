using AssistenteExecutivo.Application.Commands.Notes;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Notes;

public class UpdateNoteCommandHandler : IRequestHandler<UpdateNoteCommand>
{
    private readonly INoteRepository _noteRepository;
    private readonly IContactRepository _contactRepository;
    private readonly IClock _clock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;

    public UpdateNoteCommandHandler(
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

    public async Task Handle(UpdateNoteCommand request, CancellationToken cancellationToken)
    {
        // Buscar nota
        var note = await _noteRepository.GetByIdAsync(request.NoteId, cancellationToken);
        if (note == null)
            throw new DomainException("Domain:NoteNaoEncontrada");

        // Validar que a nota pertence ao usuário (através do contato)
        var contact = await _contactRepository.GetByIdAsync(note.ContactId, request.OwnerUserId, cancellationToken);
        if (contact == null)
            throw new DomainException("Domain:NoteNaoEncontrada");

        // Validar que o OwnerUserId do contato corresponde ao request
        if (contact.OwnerUserId != request.OwnerUserId)
            throw new DomainException("Domain:AuthorIdInvalido");

        // Atualizar raw content se fornecido
        if (!string.IsNullOrWhiteSpace(request.RawContent))
        {
            note.UpdateRawContent(request.RawContent, _clock);
        }

        // Atualizar structured data se fornecido
        if (!string.IsNullOrWhiteSpace(request.StructuredData))
        {
            note.UpdateStructuredData(request.StructuredData, _clock);
        }

        // Atualizar no repositório
        await _noteRepository.UpdateAsync(note, cancellationToken);

        // Salvar alterações
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publicar eventos de domínio
        foreach (var domainEvent in note.DomainEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }
        note.ClearDomainEvents();
    }
}

