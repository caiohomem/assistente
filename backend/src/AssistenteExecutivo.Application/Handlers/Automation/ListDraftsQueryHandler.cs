using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.Automation;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Automation;

public class ListDraftsQueryHandler : IRequestHandler<ListDraftsQuery, ListDraftsResultDto>
{
    private readonly IDraftDocumentRepository _draftRepository;

    public ListDraftsQueryHandler(IDraftDocumentRepository draftRepository)
    {
        _draftRepository = draftRepository;
    }

    public async Task<ListDraftsResultDto> Handle(ListDraftsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Max(1, Math.Min(100, request.PageSize));

        List<Domain.Entities.DraftDocument> drafts;

        if (request.ContactId.HasValue)
        {
            drafts = await _draftRepository.GetByContactIdAsync(request.ContactId.Value, request.OwnerUserId, cancellationToken);
        }
        else if (request.CompanyId.HasValue)
        {
            drafts = await _draftRepository.GetByCompanyIdAsync(request.CompanyId.Value, request.OwnerUserId, cancellationToken);
        }
        else if (request.DocumentType.HasValue)
        {
            drafts = await _draftRepository.GetByDocumentTypeAsync(request.DocumentType.Value, request.OwnerUserId, cancellationToken);
        }
        else if (request.Status.HasValue)
        {
            drafts = await _draftRepository.GetByStatusAsync(request.Status.Value, request.OwnerUserId, cancellationToken);
        }
        else
        {
            drafts = await _draftRepository.GetByOwnerIdAsync(request.OwnerUserId, cancellationToken);
        }

        var total = drafts.Count;
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);
        var skip = (page - 1) * pageSize;

        var paginatedDrafts = drafts
            .OrderByDescending(d => d.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .ToList();

        return new ListDraftsResultDto
        {
            Drafts = paginatedDrafts.Select(MapToDto).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages
        };
    }

    private static DraftDto MapToDto(Domain.Entities.DraftDocument draft)
    {
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





