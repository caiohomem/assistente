import { apiClient } from "./apiClient";

export async function completeMilestoneClient(
  milestoneId: string,
  agreementId: string,
  input?: { notes?: string; releasedPayoutTransactionId?: string },
) {
  return apiClient.post<void>(`/api/milestones/${milestoneId}/complete`, {
    agreementId,
    notes: input?.notes,
    releasedPayoutTransactionId: input?.releasedPayoutTransactionId,
  });
}

export async function triggerMilestonePayoutClient(
  milestoneId: string,
  agreementId: string,
  input: { amount?: number; currency?: string; beneficiaryPartyId?: string },
) {
  return apiClient.post<{ transactionId: string }>(`/api/milestones/${milestoneId}/trigger-payout`, {
    agreementId,
    amount: input.amount,
    currency: input.currency ?? "BRL",
    beneficiaryPartyId: input.beneficiaryPartyId,
  });
}
