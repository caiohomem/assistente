using AssistenteExecutivo.Domain.Entities;

namespace AssistenteExecutivo.Application.Interfaces;

public interface IAgreementAcceptanceWorkflowService
{
    Task StartAsync(CommissionAgreement agreement, CancellationToken cancellationToken = default);
}
