export type EscrowAccountStatus = "Active" | "Suspended" | "Closed" | number;
export type EscrowTransactionType = "Deposit" | "Payout" | "Refund" | "Fee" | number;
export type EscrowTransactionStatus = "Pending" | "Approved" | "Rejected" | "Disputed" | "Completed" | "Failed" | number;

export interface EscrowTransactionDto {
  transactionId: string;
  escrowAccountId: string;
  partyId?: string | null;
  type: EscrowTransactionType;
  amount: number;
  currency: string;
  description?: string | null;
  status: EscrowTransactionStatus;
  approvalType?: string | number | null;
  approvedBy?: string | null;
  approvedAt?: string | null;
  rejectedBy?: string | null;
  rejectionReason?: string | null;
  disputeReason?: string | null;
  stripePaymentIntentId?: string | null;
  stripeTransferId?: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface EscrowAccountDto {
  escrowAccountId: string;
  agreementId: string;
  ownerUserId: string;
  currency: string;
  status: EscrowAccountStatus;
  stripeConnectedAccountId?: string | null;
  balance: number;
  createdAt: string;
  updatedAt: string;
  transactions: EscrowTransactionDto[];
}

export interface EscrowDepositResult {
  transactionId: string;
  paymentIntentId: string;
  clientSecret: string;
}
