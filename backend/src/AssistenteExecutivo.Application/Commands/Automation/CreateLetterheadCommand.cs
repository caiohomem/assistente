using MediatR;

namespace AssistenteExecutivo.Application.Commands.Automation;

public class CreateLetterheadCommand : IRequest<Guid>
{
    public Guid OwnerUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DesignData { get; set; } = string.Empty;
}









