using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Mappings;
using AssistenteExecutivo.Application.Queries.Capture;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Capture;

public class ListCaptureJobsQueryHandler : IRequestHandler<ListCaptureJobsQuery, List<CaptureJobDto>>
{
    private readonly ICaptureJobRepository _captureJobRepository;

    public ListCaptureJobsQueryHandler(ICaptureJobRepository captureJobRepository)
    {
        _captureJobRepository = captureJobRepository;
    }

    public async Task<List<CaptureJobDto>> Handle(ListCaptureJobsQuery request, CancellationToken cancellationToken)
    {
        var jobs = await _captureJobRepository.GetAllByOwnerUserIdAsync(request.OwnerUserId, cancellationToken);

        return jobs.Select(CaptureJobMapper.MapToDto).ToList();
    }
}

