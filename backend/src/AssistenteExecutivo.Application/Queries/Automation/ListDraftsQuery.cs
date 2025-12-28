using AssistenteExecutivo.Domain.Enums;
using MediatR;

namespace AssistenteExecutivo.Application.Queries.Automation;

public class ListDraftsQuery : IRequest<ListDraftsResultDto>
{
    public Guid OwnerUserId { get; set; }
    public Guid? ContactId { get; set; }
    public Guid? CompanyId { get; set; }
    public DocumentType? DocumentType { get; set; }
    public DraftStatus? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class ListDraftsResultDto
{
    public List<DraftDto> Drafts { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class DraftDto
{
    public Guid DraftId { get; set; }
    public Guid OwnerUserId { get; set; }
    public Guid? ContactId { get; set; }
    public Guid? CompanyId { get; set; }
    public DocumentType DocumentType { get; set; }
    public Guid? TemplateId { get; set; }
    public Guid? LetterheadId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DraftStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}





