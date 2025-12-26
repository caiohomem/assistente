using MediatR;

namespace AssistenteExecutivo.Application.Commands.Notes;

public class CreateTextNoteCommand : IRequest<Guid>
{
    public Guid ContactId { get; set; }
    public Guid AuthorId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? StructuredData { get; set; }
}



