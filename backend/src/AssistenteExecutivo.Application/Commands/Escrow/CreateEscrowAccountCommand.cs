using MediatR;

namespace AssistenteExecutivo.Application.Commands.Escrow;

public class CreateEscrowAccountCommand : IRequest<Guid>
{
    public Guid EscrowAccountId { get; set; }
    public Guid AgreementId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string Currency { get; set; } = "BRL";
}
