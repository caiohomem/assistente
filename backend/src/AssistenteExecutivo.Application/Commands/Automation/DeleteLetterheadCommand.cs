using MediatR;

namespace AssistenteExecutivo.Application.Commands.Automation;

public class DeleteLetterheadCommand : IRequest
{
    public Guid LetterheadId { get; set; }
    public Guid OwnerUserId { get; set; }
}

