using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Mappings;
using AssistenteExecutivo.Application.Queries.Capture;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Capture;

public class GetCaptureJobByIdQueryHandler : IRequestHandler<GetCaptureJobByIdQuery, CaptureJobDto?>
{
    private readonly ICaptureJobRepository _captureJobRepository;

    public GetCaptureJobByIdQueryHandler(ICaptureJobRepository captureJobRepository)
    {
        _captureJobRepository = captureJobRepository;
    }

    public async Task<CaptureJobDto?> Handle(GetCaptureJobByIdQuery request, CancellationToken cancellationToken)
    {
        var job = await _captureJobRepository.GetByIdAsync(request.JobId, request.OwnerUserId, cancellationToken);

        if (job == null)
            return null;

        return CaptureJobMapper.MapToDto(job);
    }
}

