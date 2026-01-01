using MediatR;

namespace AssistenteExecutivo.Application.Queries.Automation;

public class GetLetterheadByIdQuery : IRequest<LetterheadDto?>
{
    public Guid LetterheadId { get; set; }
    public Guid OwnerUserId { get; set; }
}







