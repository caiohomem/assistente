using AssistenteExecutivo.Application.Commands.Notes;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Exceptions;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Notes;

public class DeleteNoteCommandHandler : IRequestHandler<DeleteNoteCommand>
{
    private readonly INoteRepository _noteRepository;
    private readonly IContactRepository _contactRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteNoteCommandHandler(
        INoteRepository noteRepository,
        IContactRepository contactRepository,
        IUnitOfWork unitOfWork)
    {
        _noteRepository = noteRepository;
        _contactRepository = contactRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteNoteCommand request, CancellationToken cancellationToken)
    {
        if (request.NoteId == Guid.Empty)
            throw new DomainException("Domain:NoteIdObrigatorio");

        if (request.OwnerUserId == Guid.Empty)
            throw new DomainException("Domain:OwnerUserIdObrigatorio");

        // Buscar a nota
        var note = await _noteRepository.GetByIdAsync(request.NoteId, cancellationToken);
        if (note == null)
            throw new DomainException("Domain:NoteNaoEncontrada");

        // Verificar se a nota pertence ao usuário através do contato
        var (exists, ownerUserId, isDeleted) = await _contactRepository.GetContactStatusAsync(note.ContactId, cancellationToken);
        if (!exists || isDeleted)
            throw new DomainException("Domain:ContactNaoEncontrado");

        if (ownerUserId != request.OwnerUserId)
            throw new DomainException("Domain:NoteNaoPertenceAoUsuario");

        // Deletar a nota
        await _noteRepository.DeleteAsync(note, cancellationToken);

        // Salvar alterações
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}





