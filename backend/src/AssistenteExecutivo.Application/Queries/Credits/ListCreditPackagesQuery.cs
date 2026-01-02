using AssistenteExecutivo.Application.DTOs;
using MediatR;

namespace AssistenteExecutivo.Application.Queries.Credits;

public class ListCreditPackagesQuery : IRequest<List<CreditPackageDto>>
{
    public bool IncludeInactive { get; set; } = false;
}













