using AssistenteExecutivo.Application.Commands.Automation;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Automation;

public class CreateDraftDocumentCommandHandler : IRequestHandler<CreateDraftDocumentCommand, Guid>
{
    private readonly IDraftDocumentRepository _draftRepository;
    private readonly IContactRepository _contactRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly ITemplateRepository _templateRepository;
    private readonly ILetterheadRepository _letterheadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IPublisher _publisher;

    public CreateDraftDocumentCommandHandler(
        IDraftDocumentRepository draftRepository,
        IContactRepository contactRepository,
        ICompanyRepository companyRepository,
        ITemplateRepository templateRepository,
        ILetterheadRepository letterheadRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IPublisher publisher)
    {
        _draftRepository = draftRepository;
        _contactRepository = contactRepository;
        _companyRepository = companyRepository;
        _templateRepository = templateRepository;
        _letterheadRepository = letterheadRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _publisher = publisher;
    }

    public async Task<Guid> Handle(CreateDraftDocumentCommand request, CancellationToken cancellationToken)
    {
        if (request.OwnerUserId == Guid.Empty)
            throw new DomainException("Domain:OwnerUserIdObrigatorio");

        // Validar contato se fornecido
        if (request.ContactId.HasValue)
        {
            var contact = await _contactRepository.GetByIdAsync(request.ContactId.Value, request.OwnerUserId, cancellationToken);
            if (contact == null)
                throw new DomainException("Domain:ContactNaoEncontrado");
        }

        // Validar empresa se fornecida
        if (request.CompanyId.HasValue)
        {
            var company = await _companyRepository.GetByIdAsync(request.CompanyId.Value, cancellationToken);
            if (company == null)
                throw new DomainException("Domain:CompanyNaoEncontrado");
        }

        // Validar template se fornecido
        if (request.TemplateId.HasValue)
        {
            var template = await _templateRepository.GetByIdAsync(request.TemplateId.Value, request.OwnerUserId, cancellationToken);
            if (template == null)
                throw new DomainException("Domain:TemplateNaoEncontrado");
        }

        // Validar letterhead se fornecido
        if (request.LetterheadId.HasValue)
        {
            var letterhead = await _letterheadRepository.GetByIdAsync(request.LetterheadId.Value, request.OwnerUserId, cancellationToken);
            if (letterhead == null)
                throw new DomainException("Domain:LetterheadNaoEncontrado");
        }

        var draftId = Guid.NewGuid();
        var draft = DraftDocument.Create(
            draftId,
            request.OwnerUserId,
            request.DocumentType,
            request.Content,
            _clock);

        if (request.ContactId.HasValue)
        {
            draft.AssociateContact(request.ContactId.Value, _clock);
        }

        if (request.CompanyId.HasValue)
        {
            draft.AssociateCompany(request.CompanyId.Value, _clock);
        }

        if (request.TemplateId.HasValue)
        {
            draft.SetTemplate(request.TemplateId.Value, _clock);
        }

        if (request.LetterheadId.HasValue)
        {
            draft.SetLetterhead(request.LetterheadId.Value, _clock);
        }

        await _draftRepository.AddAsync(draft, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish domain events
        foreach (var domainEvent in draft.DomainEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }
        draft.ClearDomainEvents();

        return draftId;
    }
}

