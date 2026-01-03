import { apiClient } from "./apiClient";
import type { EscrowAccountDto, EscrowDepositResult, EscrowTransactionDto } from "@/lib/types/escrow";

export async function createEscrowAccountClient(input: { agreementId: string; currency?: string }) {
  return apiClient.post<{ escrowAccountId: string }>("/api/escrow/accounts", {
    agreementId: input.agreementId,
    currency: input.currency ?? "BRL",
  });
}

export async function getEscrowAccountClient(escrowAccountId: string) {
  return apiClient.get<EscrowAccountDto>(`/api/escrow/accounts/${escrowAccountId}`);
}

export async function listEscrowTransactionsClient(escrowAccountId: string) {
  return apiClient.get<EscrowTransactionDto[]>(
    `/api/escrow/accounts/${escrowAccountId}/transactions`,
  );
}

export async function depositEscrowClient(input: {
  escrowAccountId: string;
  amount: number;
  currency?: string;
  description?: string;
}) {
  return apiClient.post<EscrowDepositResult>(
    `/api/escrow/accounts/${input.escrowAccountId}/deposit`,
    {
      amount: input.amount,
      currency: input.currency ?? "BRL",
      description: input.description,
    },
  );
}
