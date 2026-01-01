using AssistenteExecutivo.Domain.Enums;
using MediatR;

namespace AssistenteExecutivo.Application.Commands.Automation;

public class CreateDraftDocumentCommand : IRequest<Guid>
{
    public Guid OwnerUserId { get; set; }
    public DocumentType DocumentType { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid? ContactId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? TemplateId { get; set; }
    public Guid? LetterheadId { get; set; }
}







