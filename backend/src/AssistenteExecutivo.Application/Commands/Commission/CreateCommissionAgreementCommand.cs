using MediatR;

namespace AssistenteExecutivo.Application.Commands.Commission;

public class CreateCommissionAgreementCommand : IRequest<Guid>
{
    public Guid AgreementId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Terms { get; set; }
    public decimal TotalValue { get; set; }
    public string Currency { get; set; } = "BRL";
}
