using MediatR;

namespace AssistenteExecutivo.Application.Commands.Automation;

public class UpdateLetterheadCommand : IRequest
{
    public Guid LetterheadId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string? Name { get; set; }
    public string? DesignData { get; set; }
    public bool? IsActive { get; set; }
}







