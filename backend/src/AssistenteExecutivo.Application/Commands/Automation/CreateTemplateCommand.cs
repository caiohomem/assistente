using AssistenteExecutivo.Domain.Enums;
using MediatR;

namespace AssistenteExecutivo.Application.Commands.Automation;

public class CreateTemplateCommand : IRequest<Guid>
{
    public Guid OwnerUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public TemplateType Type { get; set; }
    public string Body { get; set; } = string.Empty;
    public string? PlaceholdersSchema { get; set; }
}









