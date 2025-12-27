using MediatR;

namespace AssistenteExecutivo.Application.Commands.Capture;

public class UploadCardCommand : IRequest<UploadCardCommandResult>
{
    public Guid OwnerUserId { get; set; }
    public byte[] ImageBytes { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
}

public class UploadCardCommandResult
{
    public Guid ContactId { get; set; }
    public Guid JobId { get; set; }
    public Guid MediaId { get; set; }
}






