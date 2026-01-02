using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.Automation;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Automation;

public class ListLetterheadsQueryHandler : IRequestHandler<ListLetterheadsQuery, ListLetterheadsResultDto>
{
    private readonly ILetterheadRepository _letterheadRepository;

    public ListLetterheadsQueryHandler(ILetterheadRepository letterheadRepository)
    {
        _letterheadRepository = letterheadRepository;
    }

    public async Task<ListLetterheadsResultDto> Handle(ListLetterheadsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Max(1, Math.Min(100, request.PageSize));

        List<Domain.Entities.Letterhead> letterheads;

        if (request.ActiveOnly == true)
        {
            letterheads = await _letterheadRepository.GetActiveByOwnerIdAsync(request.OwnerUserId, cancellationToken);
        }
        else
        {
            letterheads = await _letterheadRepository.GetByOwnerIdAsync(request.OwnerUserId, cancellationToken);
        }

        var total = letterheads.Count;
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);
        var skip = (page - 1) * pageSize;

        var paginatedLetterheads = letterheads
            .OrderByDescending(l => l.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .ToList();

        return new ListLetterheadsResultDto
        {
            Letterheads = paginatedLetterheads.Select(MapToDto).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages
        };
    }

    private static LetterheadDto MapToDto(Domain.Entities.Letterhead letterhead)
    {
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









