using AssistenteExecutivo.Application.DTOs;
using MediatR;

namespace AssistenteExecutivo.Application.Queries.Capture;

public class GetCaptureJobByIdQuery : IRequest<CaptureJobDto?>
{
    public Guid JobId { get; set; }
    public Guid OwnerUserId { get; set; }
}



