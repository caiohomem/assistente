using MediatR;

namespace AssistenteExecutivo.Application.Commands.Automation;

public class UpdateDraftDocumentCommand : IRequest
{
    public Guid DraftId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string? Content { get; set; }
    public Guid? ContactId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? TemplateId { get; set; }
    public Guid? LetterheadId { get; set; }
}

