using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.Automation;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Automation;

public class GetLetterheadByIdQueryHandler : IRequestHandler<GetLetterheadByIdQuery, LetterheadDto?>
{
    private readonly ILetterheadRepository _letterheadRepository;

    public GetLetterheadByIdQueryHandler(ILetterheadRepository letterheadRepository)
    {
        _letterheadRepository = letterheadRepository;
    }

    public async Task<LetterheadDto?> Handle(GetLetterheadByIdQuery request, CancellationToken cancellationToken)
    {
        var letterhead = await _letterheadRepository.GetByIdAsync(request.LetterheadId, request.OwnerUserId, cancellationToken);
        if (letterhead == null)
            return null;

        return new LetterheadDto
        {
            LetterheadId = letterhead.LetterheadId,
            OwnerUserId = letterhead.OwnerUserId,
            Name = letterhead.Name,
            DesignData = letterhead.DesignData,
            IsActive = letterhead.IsActive,
            CreatedAt = letterhead.CreatedAt,
            UpdatedAt = letterhead.UpdatedAt
        };
    }
}







