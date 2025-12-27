using MediatR;

namespace AssistenteExecutivo.Application.Commands.Automation;

public class UpdateTemplateCommand : IRequest
{
    public Guid TemplateId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string? Name { get; set; }
    public string? Body { get; set; }
    public string? PlaceholdersSchema { get; set; }
    public bool? Active { get; set; }
}

