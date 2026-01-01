using AssistenteExecutivo.Application.DTOs;
using MediatR;

namespace AssistenteExecutivo.Application.Queries.Capture;

public class ListCaptureJobsQuery : IRequest<List<CaptureJobDto>>
{
    public Guid OwnerUserId { get; set; }
}












