import { apiClient } from "./apiClient";
import type {
  CreateNegotiationSessionInput,
  NegotiationProposalDto,
  NegotiationSessionDto,
} from "@/lib/types/negotiation";

export async function listNegotiationSessionsClient() {
  return apiClient.get<NegotiationSessionDto[]>("/api/negotiations");
}

export async function getNegotiationSessionClient(sessionId: string) {
  return apiClient.get<NegotiationSessionDto>(`/api/negotiations/${sessionId}`);
}

export async function listNegotiationProposalsClient(sessionId: string) {
  return apiClient.get<NegotiationProposalDto[]>(`/api/negotiations/${sessionId}/proposals`);
}

export async function createNegotiationSessionClient(input: CreateNegotiationSessionInput) {
  return apiClient.post<{ sessionId: string }>("/api/negotiations", {
    title: input.title,
    context: input.context,
  });
}

export async function submitNegotiationProposalClient(input: {
  sessionId: string;
  partyId?: string;
  content: string;
}) {
  return apiClient.post<{ proposalId: string }>(`/api/negotiations/${input.sessionId}/proposals`, {
    partyId: input.partyId,
    content: input.content,
  });
}

export async function requestAiProposalClient(input: { sessionId: string; instructions?: string }) {
  return apiClient.post<{ proposalId: string }>(
    `/api/negotiations/${input.sessionId}/proposals/ai-suggest`,
    {
      instructions: input.instructions,
    },
  );
}

export async function acceptNegotiationProposalClient(input: {
  sessionId: string;
  proposalId: string;
  actingPartyId?: string;
}) {
  return apiClient.post<void>(
    `/api/negotiations/${input.sessionId}/proposals/${input.proposalId}/accept`,
    { actingPartyId: input.actingPartyId },
  );
}

export async function rejectNegotiationProposalClient(input: {
  sessionId: string;
  proposalId: string;
  reason: string;
  actingPartyId?: string;
}) {
  return apiClient.post<void>(
    `/api/negotiations/${input.sessionId}/proposals/${input.proposalId}/reject`,
    { reason: input.reason, actingPartyId: input.actingPartyId },
  );
}
