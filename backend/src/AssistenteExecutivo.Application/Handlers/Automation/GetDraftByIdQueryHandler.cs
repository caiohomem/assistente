using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.Automation;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Automation;

public class GetDraftByIdQueryHandler : IRequestHandler<GetDraftByIdQuery, DraftDto?>
{
    private readonly IDraftDocumentRepository _draftRepository;

    public GetDraftByIdQueryHandler(IDraftDocumentRepository draftRepository)
    {
        _draftRepository = draftRepository;
    }

    public async Task<DraftDto?> Handle(GetDraftByIdQuery request, CancellationToken cancellationToken)
    {
        var draft = await _draftRepository.GetByIdAsync(request.DraftId, request.OwnerUserId, cancellationToken);
        if (draft == null)
            return null;

        return new DraftDto
        {
            DraftId = draft.DraftId,
            OwnerUserId = draft.OwnerUserId,
            ContactId = draft.ContactId,
            CompanyId = draft.CompanyId,
            DocumentType = draft.DocumentType,
            TemplateId = draft.TemplateId,
            LetterheadId = draft.LetterheadId,
            Content = draft.Content,
            Status = draft.Status,
            CreatedAt = draft.CreatedAt,
            UpdatedAt = draft.UpdatedAt
        };
    }
}







