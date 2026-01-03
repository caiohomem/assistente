export type NegotiationStatus = "Open" | "Resolved" | "Closed" | "AgreementGenerated" | number;
export type ProposalSource = "Party" | "AI" | "Mediator" | number;
export type ProposalStatus = "Pending" | "Accepted" | "Rejected" | "Superseded" | number;

export interface NegotiationProposalDto {
  proposalId: string;
  sessionId: string;
  partyId?: string | null;
  source: ProposalSource;
  status: ProposalStatus;
  content: string;
  rejectionReason?: string | null;
  createdAt: string;
  respondedAt?: string | null;
}

export interface NegotiationSessionDto {
  sessionId: string;
  ownerUserId: string;
  title: string;
  context?: string | null;
  status: NegotiationStatus;
  generatedAgreementId?: string | null;
  createdAt: string;
  updatedAt: string;
  lastAiSuggestionRequestedAt?: string | null;
  nextAiSuggestionAvailableAt?: string | null;
  aiSuggestionCooldownActive?: boolean;
  pendingProposalCount?: number;
  aiProposalCount?: number;
  proposals: NegotiationProposalDto[];
}

export interface CreateNegotiationSessionInput {
  title: string;
  context?: string;
}
