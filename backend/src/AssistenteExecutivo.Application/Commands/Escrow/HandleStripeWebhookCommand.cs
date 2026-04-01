using MediatR;

namespace AssistenteExecutivo.Application.Commands.Escrow;

public class HandleStripeWebhookCommand : IRequest<Unit>
{
    public string Payload { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
}
