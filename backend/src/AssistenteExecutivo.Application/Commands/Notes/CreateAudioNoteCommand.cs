using MediatR;

namespace AssistenteExecutivo.Application.Commands.Notes;

public class CreateAudioNoteCommand : IRequest<Guid>
{
    public Guid ContactId { get; set; }
    public Guid AuthorId { get; set; }
    public string Transcript { get; set; } = string.Empty;
    public string? StructuredData { get; set; }
}










