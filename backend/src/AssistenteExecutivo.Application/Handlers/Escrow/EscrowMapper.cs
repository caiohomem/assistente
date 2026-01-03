using System.Linq;
using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Domain.Entities;

namespace AssistenteExecutivo.Application.Handlers.Escrow;

internal static class EscrowMapper
{
    public static EscrowAccountDto Map(EscrowAccount account)
    {
        return new EscrowAccountDto
        {
            EscrowAccountId = account.EscrowAccountId,
            AgreementId = account.AgreementId,
            OwnerUserId = account.OwnerUserId,
            Currency = account.Currency,
            Status = account.Status,
            StripeConnectedAccountId = account.StripeConnectedAccountId,
            Balance = account.Balance.Amount,
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.UpdatedAt,
            Transactions = account.Transactions
                .OrderByDescending(t => t.CreatedAt)
                .Select(MapTransaction)
                .ToList()
        };
    }

    public static EscrowTransactionDto MapTransaction(EscrowTransaction transaction)
    {
        return new EscrowTransactionDto
        {
            TransactionId = transaction.TransactionId,
            EscrowAccountId = transaction.EscrowAccountId,
            PartyId = transaction.PartyId,
            Type = transaction.Type,
            Amount = transaction.Amount.Amount,
            Currency = transaction.Amount.Currency,
            Description = transaction.Description,
            Status = transaction.Status,
            ApprovalType = transaction.ApprovalType,
            ApprovedBy = transaction.ApprovedBy,
            ApprovedAt = transaction.ApprovedAt,
            RejectedBy = transaction.RejectedBy,
            RejectionReason = transaction.RejectionReason,
            DisputeReason = transaction.DisputeReason,
            StripePaymentIntentId = transaction.StripePaymentIntentId,
            StripeTransferId = transaction.StripeTransferId,
            CreatedAt = transaction.CreatedAt,
            UpdatedAt = transaction.UpdatedAt
        };
    }
}
