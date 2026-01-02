using MediatR;

namespace AssistenteExecutivo.Application.Commands.Credits;

public class PurchaseCreditPackageCommand : IRequest<PurchaseCreditPackageResult>
{
    public Guid OwnerUserId { get; set; }
    public Guid PackageId { get; set; }
}

public class PurchaseCreditPackageResult
{
    public Guid OwnerUserId { get; set; }
    public decimal NewBalance { get; set; }
    public Guid TransactionId { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public decimal CreditsAdded { get; set; }
}













