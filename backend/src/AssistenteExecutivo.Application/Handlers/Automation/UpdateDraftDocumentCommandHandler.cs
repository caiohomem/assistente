using AssistenteExecutivo.Application.Commands.Automation;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Automation;

public class UpdateDraftDocumentCommandHandler : IRequestHandler<UpdateDraftDocumentCommand>
{
    private readonly IDraftDocumentRepository _draftRepository;
    private readonly IContactRepository _contactRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly ITemplateRepository _templateRepository;
    private readonly ILetterheadRepository _letterheadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpdateDraftDocumentCommandHandler(
        IDraftDocumentRepository draftRepository,
        IContactRepository contactRepository,
        ICompanyRepository companyRepository,
        ITemplateRepository templateRepository,
        ILetterheadRepository letterheadRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _draftRepository = draftRepository;
        _contactRepository = contactRepository;
        _companyRepository = companyRepository;
        _templateRepository = templateRepository;
        _letterheadRepository = letterheadRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task Handle(UpdateDraftDocumentCommand request, CancellationToken cancellationToken)
    {
        var draft = await _draftRepository.GetByIdAsync(request.DraftId, request.OwnerUserId, cancellationToken);
        if (draft == null)
            throw new DomainException("Domain:DraftNaoEncontrado");

        if (!string.IsNullOrWhiteSpace(request.Content))
        {
            draft.UpdateContent(request.Content, _clock);
        }

        if (request.ContactId.HasValue)
        {
            var contact = await _contactRepository.GetByIdAsync(request.ContactId.Value, request.OwnerUserId, cancellationToken);
            if (contact == null)
                throw new DomainException("Domain:ContactNaoEncontrado");
            draft.AssociateContact(request.ContactId.Value, _clock);
        }

        if (request.CompanyId.HasValue)
        {
            var company = await _companyRepository.GetByIdAsync(request.CompanyId.Value, cancellationToken);
            if (company == null)
                throw new DomainException("Domain:CompanyNaoEncontrado");
            draft.AssociateCompany(request.CompanyId.Value, _clock);
        }

        if (request.TemplateId.HasValue)
        {
            var template = await _templateRepository.GetByIdAsync(request.TemplateId.Value, request.OwnerUserId, cancellationToken);
            if (template == null)
                throw new DomainException("Domain:TemplateNaoEncontrado");
            draft.SetTemplate(request.TemplateId.Value, _clock);
        }

        if (request.LetterheadId.HasValue)
        {
            var letterhead = await _letterheadRepository.GetByIdAsync(request.LetterheadId.Value, request.OwnerUserId, cancellationToken);
            if (letterhead == null)
                throw new DomainException("Domain:LetterheadNaoEncontrado");
            draft.SetLetterhead(request.LetterheadId.Value, _clock);
        }

        await _draftRepository.UpdateAsync(draft, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}





