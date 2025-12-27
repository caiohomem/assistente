using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Interfaces;
using MediatR;

namespace AssistenteExecutivo.Application.Queries.Plans;

public class ListPlansQuery : IRequest<List<PlanDto>>
{
    public bool IncludeInactive { get; set; } = false;
}

public class ListPlansQueryHandler : IRequestHandler<ListPlansQuery, List<PlanDto>>
{
    private readonly IPlanRepository _planRepository;

    public ListPlansQueryHandler(IPlanRepository planRepository)
    {
        _planRepository = planRepository;
    }

    public async Task<List<PlanDto>> Handle(ListPlansQuery request, CancellationToken cancellationToken)
    {
        var plans = await _planRepository.GetAllAsync(request.IncludeInactive, cancellationToken);

        return plans.Select(p => new PlanDto
        {
            PlanId = p.PlanId,
            Name = p.Name,
            Price = p.Price,
            Currency = p.Currency,
            Interval = p.Interval.ToString().ToLowerInvariant(),
            Features = p.Features.ToList(),
            Limits = p.Limits != null ? new PlanLimitsDto
            {
                Contacts = p.Limits.Contacts,
                Notes = p.Limits.Notes,
                CreditsPerMonth = p.Limits.CreditsPerMonth,
                StorageGB = p.Limits.StorageGB
            } : null,
            IsActive = p.IsActive,
            Highlighted = p.Highlighted
        }).ToList();
    }
}





