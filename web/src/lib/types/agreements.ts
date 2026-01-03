export type AgreementStatus = "Draft" | "Active" | "Completed" | "Disputed" | "Canceled" | number;
export type PartyRole = "Seller" | "Buyer" | "Broker" | "Agent" | "Witness" | number;
export type MilestoneStatus = "Pending" | "Completed" | "Overdue" | number;

export interface AgreementPartyDto {
  partyId: string;
  contactId?: string | null;
  companyId?: string | null;
  partyName: string;
  email?: string | null;
  splitPercentage: number;
  role: PartyRole;
  hasAccepted: boolean;
  acceptedAt?: string | null;
}

export interface MilestoneDto {
  milestoneId: string;
  agreementId: string;
  description: string;
  value: number;
  currency: string;
  dueDate: string;
  status: MilestoneStatus;
  createdAt: string;
  completedAt?: string | null;
  completionNotes?: string | null;
  releasedPayoutTransactionId?: string | null;
}

export interface CommissionAgreementDto {
  agreementId: string;
  ownerUserId: string;
  title: string;
  description?: string | null;
  terms?: string | null;
  totalValue: number;
  currency: string;
  status: AgreementStatus;
  escrowAccountId?: string | null;
  createdAt: string;
  updatedAt: string;
  activatedAt?: string | null;
  completedAt?: string | null;
  canceledAt?: string | null;
  parties: AgreementPartyDto[];
  milestones: MilestoneDto[];
}

export const AgreementStatusLabels: Record<number, string> = {
  1: "Draft",
  2: "Active",
  3: "Completed",
  4: "Disputed",
  5: "Canceled",
};

export const PartyRoleLabels: Record<number, string> = {
  1: "Seller",
  2: "Buyer",
  3: "Broker",
  4: "Agent",
  5: "Witness",
};

export const MilestoneStatusLabels: Record<number, string> = {
  1: "Pending",
  2: "Completed",
  3: "Overdue",
};

export const getStatusLabel = (value: AgreementStatus) => {
  if (typeof value === "number") {
    return AgreementStatusLabels[value] ?? `#${value}`;
  }
  return value;
};

export const getRoleLabel = (value: PartyRole) => {
  if (typeof value === "number") {
    return PartyRoleLabels[value] ?? `#${value}`;
  }
  return value;
};

export const getMilestoneStatusLabel = (value: MilestoneStatus) => {
  if (typeof value === "number") {
    return MilestoneStatusLabels[value] ?? `#${value}`;
  }
  return value;
};

export interface CreateCommissionAgreementInput {
  title: string;
  description?: string;
  terms?: string;
  totalValue: number;
  currency?: string;
}

export interface AgreementWizardPartyInput {
  partyId?: string;
  contactId?: string;
  partyName: string;
  email?: string;
  splitPercentage: number;
  role?: PartyRole;
}

export interface AgreementWizardMilestoneInput {
  milestoneId?: string;
  description: string;
  value: number;
  currency?: string;
  dueDate: string;
}
