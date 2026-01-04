import { apiClient } from "./apiClient";
import type {
  AgreementAcceptanceStatusDto,
  AgreementWizardMilestoneInput,
  AgreementWizardPartyInput,
  CommissionAgreementDto,
  CreateCommissionAgreementInput,
} from "@/lib/types/agreements";

type RawAgreement = Record<
  string,
  unknown
> & {
  agreementId?: string;
  AgreementId?: string;
  id?: string;
  Id?: string;
};

const normalizeAgreement = (raw: RawAgreement): CommissionAgreementDto => {
  const normalized = { ...raw } as unknown as CommissionAgreementDto;
  normalized.agreementId =
    (raw.agreementId ??
      raw.AgreementId ??
      raw.id ??
      raw.Id ??
      "") as string;
  return normalized;
};

export async function listCommissionAgreementsClient(params?: { status?: string }) {
  const query = new URLSearchParams();
  if (params?.status) {
    query.set("status", params.status);
  }
  const q = query.toString();
  const path = q ? `/api/commission-agreements?${q}` : "/api/commission-agreements";
  const data = await apiClient.get<RawAgreement[]>(path);
  return data.map(normalizeAgreement);
}

export async function getCommissionAgreementClient(agreementId: string) {
  const data = await apiClient.get<RawAgreement>(`/api/commission-agreements/${agreementId}`);
  return normalizeAgreement(data);
}

export async function createCommissionAgreementClient(input: CreateCommissionAgreementInput) {
  return apiClient.post<{ agreementId: string } | { sessionId?: string; id?: string }>(
    "/api/commission-agreements",
    {
      title: input.title,
      description: input.description,
      terms: input.terms,
      totalValue: input.totalValue,
      currency: input.currency ?? "BRL",
    },
  );
}

export async function addAgreementPartyClient(
  agreementId: string,
  party: AgreementWizardPartyInput,
) {
  const roleMap: Record<string, number> = {
    Seller: 1,
    Buyer: 2,
    Broker: 3,
    Agent: 4,
    Witness: 5,
  };
  const roleValue =
    typeof party.role === "number"
      ? party.role
      : roleMap[party.role ?? "Agent"] ?? 4;
  const payload = {
    partyId: party.partyId,
    contactId: party.contactId,
    partyName: party.partyName,
    email: party.email,
    splitPercentage: party.splitPercentage,
    role: roleValue,
  };
  return apiClient.post<void>(`/api/commission-agreements/${agreementId}/parties`, payload);
}

export async function createAgreementMilestoneClient(
  agreementId: string,
  milestone: AgreementWizardMilestoneInput,
) {
  return apiClient.post<{ milestoneId: string }>(
    `/api/commission-agreements/${agreementId}/milestones`,
    {
      milestoneId: milestone.milestoneId,
      description: milestone.description,
      value: milestone.value,
      currency: milestone.currency ?? "BRL",
      dueDate: milestone.dueDate,
    },
  );
}

export async function activateAgreementClient(agreementId: string) {
  return apiClient.post<void>(`/api/commission-agreements/${agreementId}/activate`);
}

export async function completeAgreementClient(agreementId: string) {
  return apiClient.post<void>(`/api/commission-agreements/${agreementId}/complete`);
}

export async function cancelAgreementClient(agreementId: string, reason: string) {
  return apiClient.post<void>(`/api/commission-agreements/${agreementId}/cancel`, { reason });
}

export async function disputeAgreementClient(agreementId: string, reason: string) {
  return apiClient.post<void>(`/api/commission-agreements/${agreementId}/dispute`, { reason });
}

export async function getAcceptanceStatusClient(agreementId: string) {
  return apiClient.get<AgreementAcceptanceStatusDto>(
    `/api/commission-agreements/${agreementId}/acceptance/status`,
  );
}

export async function connectPartyStripeAccountClient(
  agreementId: string,
  partyId: string,
  accountIdOrCode: string
) {
  return apiClient.post<void>(
    `/api/commission-agreements/${agreementId}/parties/${partyId}/connect-stripe`,
    { authorizationCodeOrAccountId: accountIdOrCode }
  );
}
