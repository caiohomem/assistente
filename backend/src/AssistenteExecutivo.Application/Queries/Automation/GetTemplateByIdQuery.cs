using MediatR;

namespace AssistenteExecutivo.Application.Queries.Automation;

public class GetTemplateByIdQuery : IRequest<TemplateDto?>
{
    public Guid TemplateId { get; set; }
    public Guid OwnerUserId { get; set; }
}









