using MediatR;

namespace AssistenteExecutivo.Application.Queries.Automation;

public class GetDraftByIdQuery : IRequest<DraftDto?>
{
    public Guid DraftId { get; set; }
    public Guid OwnerUserId { get; set; }
}









