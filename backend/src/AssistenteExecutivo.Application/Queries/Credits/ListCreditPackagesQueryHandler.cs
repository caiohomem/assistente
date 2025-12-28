using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssistenteExecutivo.Application.Queries.Credits;

public class ListCreditPackagesQueryHandler : IRequestHandler<ListCreditPackagesQuery, List<CreditPackageDto>>
{
    private readonly IApplicationDbContext _context;

    public ListCreditPackagesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<CreditPackageDto>> Handle(ListCreditPackagesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.CreditPackages.AsQueryable();

        if (!request.IncludeInactive)
        {
            query = query.Where(p => p.IsActive);
        }

        var packages = await query
            .OrderBy(p => p.Price)
            .ToListAsync(cancellationToken);

        return packages.Select(p => new CreditPackageDto
        {
            PackageId = p.PackageId,
            Name = p.Name,
            Amount = p.Amount,
            Price = p.Price,
            Currency = p.Currency,
            Description = p.Description,
            IsActive = p.IsActive
        }).ToList();
    }
}









