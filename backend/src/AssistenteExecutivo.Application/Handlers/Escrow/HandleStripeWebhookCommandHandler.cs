using AssistenteExecutivo.Application.Commands.Escrow;
using AssistenteExecutivo.Domain.Interfaces;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Escrow;

public class HandleStripeWebhookCommandHandler : IRequestHandler<HandleStripeWebhookCommand, Unit>
{
    private readonly IPaymentGateway _paymentGateway;

    public HandleStripeWebhookCommandHandler(IPaymentGateway paymentGateway)
    {
        _paymentGateway = paymentGateway;
    }

    public async Task<Unit> Handle(HandleStripeWebhookCommand request, CancellationToken cancellationToken)
    {
        await _paymentGateway.HandleWebhookAsync(request.Payload, request.Signature, cancellationToken);
        return Unit.Value;
    }
}
