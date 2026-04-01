using AssistenteExecutivo.Application.Commands.Escrow;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Escrow;

public class ConnectStripeAccountCommandHandler : IRequestHandler<ConnectStripeAccountCommand, Unit>
{
    private readonly IEscrowAccountRepository _escrowAccountRepository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IUnitOfWork _unitOfWork;

    public ConnectStripeAccountCommandHandler(
        IEscrowAccountRepository escrowAccountRepository,
        IPaymentGateway paymentGateway,
        IUnitOfWork unitOfWork)
    {
        _escrowAccountRepository = escrowAccountRepository;
        _paymentGateway = paymentGateway;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(ConnectStripeAccountCommand request, CancellationToken cancellationToken)
    {
        if (request.OwnerUserId == Guid.Empty)
            throw new DomainException("Domain:UsuarioObrigatorio");
        if (string.IsNullOrWhiteSpace(request.AuthorizationCode))
            throw new DomainException("Domain:CodigoAutorizacaoObrigatorio");

        var account = await _escrowAccountRepository.GetByIdAsync(request.EscrowAccountId, cancellationToken)
            ?? throw new DomainException("Domain:ContaEscrowNaoEncontrada");

        if (account.OwnerUserId != request.OwnerUserId)
            throw new DomainException("Domain:UsuarioNaoAutorizado");

        string connectedAccountId;

        // If the code starts with "acct_", it's already a Stripe Account ID (for testing/manual setup)
        if (request.AuthorizationCode.StartsWith("acct_", StringComparison.OrdinalIgnoreCase))
        {
            connectedAccountId = request.AuthorizationCode;
        }
        else
        {
            // Otherwise, it's an OAuth authorization code that needs to be exchanged
            connectedAccountId = await _paymentGateway.ConnectAccountAsync(
                request.OwnerUserId,
                request.AuthorizationCode,
                cancellationToken);
        }

        account.ConnectStripeAccount(connectedAccountId);

        await _escrowAccountRepository.UpdateAsync(account, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
