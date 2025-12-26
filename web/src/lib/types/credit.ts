export enum CreditTransactionType {
  Grant = 1,
  Purchase = 2,
  Reserve = 3,
  Consume = 4,
  Refund = 5,
  Expire = 6,
}

export interface CreditBalance {
  ownerUserId: string;
  balance: number;
  createdAt: string;
  transactionCount: number;
}

export interface CreditTransaction {
  transactionId: string;
  ownerUserId: string;
  type: CreditTransactionType;
  amount: number;
  reason?: string | null;
  occurredAt: string;
  idempotencyKey?: string | null;
}

export interface GrantCreditsRequest {
  userId: string;
  amount: number;
  reason?: string | null;
  idempotencyKey?: string | null;
}


