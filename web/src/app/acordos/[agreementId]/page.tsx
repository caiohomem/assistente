"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { LayoutWrapper } from "@/components/LayoutWrapper";
import { Button } from "@/components/ui/button";
import {
  addAgreementPartyClient,
  cancelAgreementClient,
  completeAgreementClient,
  connectPartyStripeAccountClient,
  createAgreementMilestoneClient,
  disputeAgreementClient,
  getAcceptanceStatusClient,
  getCommissionAgreementClient,
} from "@/lib/api/agreementsApiClient";
import {
  completeMilestoneClient,
  triggerMilestonePayoutClient,
} from "@/lib/api/milestonesApiClient";
import {
  createEscrowAccountClient,
  getEscrowAccountClient,
} from "@/lib/api/escrowApiClient";
import type {
  AgreementAcceptanceStatusDto,
  AgreementWizardMilestoneInput,
  AgreementWizardPartyInput,
  CommissionAgreementDto,
  MilestoneDto,
  PartyRole,
} from "@/lib/types/agreements";
import type { EscrowAccountDto } from "@/lib/types/escrow";
import { getMilestoneStatusLabel, getRoleLabel, getStatusLabel } from "@/lib/types/agreements";
import { ApiError, extractApiFieldErrors } from "@/lib/api/types";
import NumericInput from "@/components/NumericInput";

const CURRENCY_OPTIONS = [
  { code: "BRL", flag: "🇧🇷", label: "Real brasileiro (R$)" },
  { code: "USD", flag: "🇺🇸", label: "Dólar americano (US$)" },
  { code: "EUR", flag: "🇪🇺", label: "Euro (€)" },
  { code: "GBP", flag: "🇬🇧", label: "Libra esterlina (£)" },
  { code: "CAD", flag: "🇨🇦", label: "Dólar canadense (CA$)" },
  { code: "AUD", flag: "🇦🇺", label: "Dólar australiano (A$)" },
];

export default function AgreementDetailPage() {
  const params = useParams();
  const router = useRouter();
  const agreementId = useMemo(() => {
    const value = params?.agreementId;
    return Array.isArray(value) ? value[0] : value;
  }, [params]);
  const [agreement, setAgreement] = useState<CommissionAgreementDto | null>(null);
  const [escrowAccount, setEscrowAccount] = useState<EscrowAccountDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [partyForm, setPartyForm] = useState<AgreementWizardPartyInput>({
    partyName: "",
    email: "",
    splitPercentage: 0,
    role: "Agent",
  });
  const [partyErrors, setPartyErrors] = useState<
    Partial<Record<keyof AgreementWizardPartyInput, string>>
  >({});
  const [milestoneForm, setMilestoneForm] = useState<AgreementWizardMilestoneInput>({
    description: "",
    value: 0,
    currency: "BRL",
    dueDate: "",
  });
  const [acceptanceStatus, setAcceptanceStatus] = useState<AgreementAcceptanceStatusDto | null>(null);
  const [selectedMilestone, setSelectedMilestone] = useState<MilestoneDto | null>(null);
  const [completeNotes, setCompleteNotes] = useState("");
  const [disputeReason, setDisputeReason] = useState("");
  const [cancelReason, setCancelReason] = useState("");
  const [payoutAmount, setPayoutAmount] = useState<number>();
  const [payoutCurrency, setPayoutCurrency] = useState("BRL");
  const [message, setMessage] = useState<string | null>(null);
  const [showStripeModal, setShowStripeModal] = useState(false);
  const [selectedPartyForStripe, setSelectedPartyForStripe] = useState<string | null>(null);
  const [stripeAccountInput, setStripeAccountInput] = useState("");
  const [showAddPartyModal, setShowAddPartyModal] = useState(false);
  const [showAddMilestoneModal, setShowAddMilestoneModal] = useState(false);
  const [showMoreMenu, setShowMoreMenu] = useState(false);
  const [showDisputeModal, setShowDisputeModal] = useState(false);
  const [showCancelModal, setShowCancelModal] = useState(false);
  const agreementMilestones = useMemo(
    () => agreement?.milestones ?? [],
    [agreement],
  );
  const payoutTransaction = useMemo(() => {
    if (!agreement || !escrowAccount || !selectedMilestone) return null;
    const txId = selectedMilestone.releasedPayoutTransactionId;
    if (!txId) return null;
    return escrowAccount.transactions.find((tx) => tx.transactionId === txId) ?? null;
  }, [agreement, escrowAccount, selectedMilestone]);
  const payoutBaseAmount = payoutTransaction?.amount ?? selectedMilestone?.value ?? 0;
  const payoutCurrencyDisplay =
    payoutTransaction?.currency ?? selectedMilestone?.currency ?? agreement?.currency ?? "BRL";
  const getTransactionStatusLabel = (status: string | number): string => {
    const statusMap: Record<string | number, string> = {
      "Pending": "Pendente",
      "Approved": "Aprovado",
      "Rejected": "Rejeitado",
      "Disputed": "Disputado",
      "Completed": "Concluído",
      "Failed": "Falhou",
      0: "Pendente",
      1: "Aprovado",
      2: "Rejeitado",
      3: "Disputado",
      4: "Concluído",
      5: "Falhou",
    };
    return statusMap[status] || String(status);
  };

  const payoutSplits = useMemo(() => {
    if (!agreement) return [];
    return agreement.parties.map((party) => {
      const shareAmount = (payoutBaseAmount * party.splitPercentage) / 100;
      const hasStripe = Boolean(party.stripeAccountId);
      const share = Number.isFinite(shareAmount) ? shareAmount : 0;
      const statusLabel = hasStripe
        ? payoutTransaction
          ? getTransactionStatusLabel(payoutTransaction.status)
          : "Pronto para pagar"
        : "Conta Stripe pendente";
      return {
        partyId: party.partyId,
        partyName: party.partyName,
        email: party.email,
        splitPercentage: party.splitPercentage,
        share,
        statusLabel,
      };
    });
  }, [agreement, payoutBaseAmount, payoutTransaction]);

  const isDraft = agreement?.status === 0 || agreement?.status === "Draft";
  const isActive = agreement?.status === 2 || agreement?.status === "Active";
  const loadAgreement = useCallback(async () => {
    setLoading(true);
    setError(null);
    setEscrowAccount(null);
    setAcceptanceStatus(null);
    if (!agreementId) return;
    try {
      const data = await getCommissionAgreementClient(agreementId);
      const resolvedEscrowAccountId =
        data.escrowAccountId && data.escrowAccountId !== "undefined" && data.escrowAccountId !== "null"
          ? data.escrowAccountId
          : null;
      setAgreement({ ...data, escrowAccountId: resolvedEscrowAccountId });
      setMilestoneForm((prev) => ({ ...prev, currency: data.currency }));
      if (resolvedEscrowAccountId) {
        const escrow = await getEscrowAccountClient(resolvedEscrowAccountId);
        setEscrowAccount(escrow);
      }
      try {
        const status = await getAcceptanceStatusClient(agreementId);
        setAcceptanceStatus(status);
      } catch {
        // Silently ignore
      }
    } catch (err) {
      console.error(err);
      setError(err instanceof Error ? err.message : "Erro ao carregar o acordo.");
      setEscrowAccount(null);
    } finally {
      setLoading(false);
    }
  }, [agreementId]);

  useEffect(() => {
    if (!agreementId) {
      setError("Acordo não encontrado.");
      setLoading(false);
      return;
    }
    loadAgreement();
  }, [agreementId, loadAgreement]);

  useEffect(() => {
    if (agreementMilestones.length === 0) {
      setSelectedMilestone(null);
      return;
    }
    setSelectedMilestone((prev) => {
      if (prev && agreementMilestones.some((m) => m.milestoneId === prev.milestoneId)) {
        return prev;
      }
      return agreementMilestones[0];
    });
  }, [agreementMilestones]);

  async function handleAddParty() {
    if (!agreementId) return;
    setPartyErrors({});
    setError(null);
    if (!partyForm.partyName || partyForm.splitPercentage <= 0) {
      setPartyErrors({
        partyName: !partyForm.partyName ? "Informe o nome da parte." : undefined,
        splitPercentage:
          partyForm.splitPercentage <= 0 ? "Informe um percentual válido." : undefined,
      });
      return;
    }
    try {
      await addAgreementPartyClient(agreementId, { ...partyForm, partyId: crypto.randomUUID() });
      setPartyForm({ partyName: "", email: "", splitPercentage: 0, role: "Agent" });
      setPartyErrors({});
      setShowAddPartyModal(false);
      setMessage("Parte adicionada com sucesso!");
      await loadAgreement();
    } catch (err) {
      if (err instanceof ApiError) {
        const fieldErrors = extractApiFieldErrors(err.payload);
        if (Object.keys(fieldErrors).length > 0) {
          setPartyErrors(fieldErrors as Partial<Record<keyof AgreementWizardPartyInput, string>>);
          return;
        }
      }
      setError(err instanceof Error ? err.message : "Falha ao adicionar a parte.");
    }
  }

  async function handleAddMilestone() {
    if (!agreementId) return;
    if (!milestoneForm.description || milestoneForm.value <= 0 || !milestoneForm.dueDate) {
      setError("Descrição, valor e data são obrigatórios para o milestone.");
      return;
    }
    try {
      await createAgreementMilestoneClient(agreementId, {
        ...milestoneForm,
        milestoneId: crypto.randomUUID(),
      });
      setMilestoneForm({
        description: "",
        value: 0,
        currency: agreement?.currency ?? "BRL",
        dueDate: "",
      });
      setShowAddMilestoneModal(false);
      setMessage("Milestone adicionado com sucesso!");
      await loadAgreement();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Falha ao adicionar milestone.");
    }
  }

  async function handleCompleteAgreement() {
    if (!agreementId) return;
    try {
      await completeAgreementClient(agreementId);
      setMessage("Acordo concluído com sucesso.");
      await loadAgreement();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Erro ao concluir acordo.");
    }
  }

  async function handleDispute() {
    if (!agreementId) return;
    if (!disputeReason) {
      setError("Informe o motivo da disputa.");
      return;
    }
    try {
      await disputeAgreementClient(agreementId, disputeReason);
      setDisputeReason("");
      await loadAgreement();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Erro ao abrir disputa.");
    }
  }

  async function handleCancel() {
    if (!agreementId) return;
    if (!cancelReason) {
      setError("Informe o motivo do cancelamento.");
      return;
    }
    try {
      await cancelAgreementClient(agreementId, cancelReason);
      setCancelReason("");
      await loadAgreement();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Erro ao cancelar acordo.");
    }
  }

  async function handleMilestoneComplete() {
    if (!agreementId) return;
    if (!selectedMilestone) {
      setError("Selecione um milestone para concluir.");
      return;
    }
    try {
      await completeMilestoneClient(selectedMilestone.milestoneId, agreementId, {
        notes: completeNotes,
      });
      setSelectedMilestone(null);
      setCompleteNotes("");
      await loadAgreement();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Erro ao concluir milestone.");
    }
  }

  async function handleMilestonePayout() {
    if (!agreementId) return;
    if (!selectedMilestone) {
      setError("Selecione um milestone para pagar.");
      return;
    }
    if (!agreement?.escrowAccountId) {
      setError("Crie a conta escrow antes de liberar payouts.");
      return;
    }
    try {
      await triggerMilestonePayoutClient(selectedMilestone.milestoneId, agreementId, {
        amount: payoutAmount ?? undefined,
        currency: payoutCurrency || agreement.currency,
      });
      setPayoutAmount(undefined);
      setSelectedMilestone(null);
      await loadAgreement();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Erro ao solicitar payout.");
    }
  }

  async function handleCreateEscrow() {
    if (!agreementId) return;
    if (!agreement) return;
    try {
      const response = await createEscrowAccountClient({
        agreementId: agreement.agreementId,
        currency: agreement.currency,
      });
      if ("escrowAccountId" in response) {
        await loadAgreement();
        router.push(`/escrow/${response.escrowAccountId}`);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Erro ao criar conta escrow.");
    }
  }

  async function handleConnectStripe() {
    if (!agreementId || !selectedPartyForStripe || !stripeAccountInput) {
      setError("Selecione uma parte e informe o Account ID.");
      return;
    }
    try {
      await connectPartyStripeAccountClient(
        agreementId,
        selectedPartyForStripe,
        stripeAccountInput
      );
      setMessage("Stripe conectado com sucesso!");
      setShowStripeModal(false);
      setSelectedPartyForStripe(null);
      setStripeAccountInput("");
      await loadAgreement();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Erro ao conectar Stripe.");
    }
  }

  const formatCurrency = (value: number, currency: string) => {
    try {
      return new Intl.NumberFormat("pt-BR", {
        style: "currency",
        currency,
      }).format(value);
    } catch {
      return `${currency} ${value.toFixed(2)}`;
    }
  };

  if (!agreementId) {
    return (
      <LayoutWrapper title="Detalhes do acordo" subtitle="" activeTab="agreements">
        <div className="text-sm text-destructive">
          {error ?? "Acordo não encontrado. Volte para a lista."}
        </div>
      </LayoutWrapper>
    );
  }

  if (loading) {
    return (
      <LayoutWrapper title="Detalhes do acordo" subtitle="" activeTab="agreements">
        <div className="text-sm text-muted-foreground">Carregando...</div>
      </LayoutWrapper>
    );
  }

  if (!agreement) {
    return (
      <LayoutWrapper title="Detalhes do acordo" subtitle="" activeTab="agreements">
        <div className="text-sm text-destructive">
          {error ?? "Acordo não encontrado. Volte para a lista."}
        </div>
      </LayoutWrapper>
    );
  }

  return (
    <LayoutWrapper
      title={agreement.title}
      subtitle="Gerencie partes, milestones e fluxos financeiros deste acordo."
      activeTab="agreements"
    >
      <div className="space-y-6">
        {error && (
          <div className="rounded-xl border border-red-200 bg-red-50/80 text-red-700 px-4 py-3 text-sm">
            {error}
          </div>
        )}
        {message && (
          <div className="rounded-xl border border-emerald-200 bg-emerald-50/80 text-emerald-700 px-4 py-3 text-sm">
            {message}
          </div>
        )}

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
          <div className="rounded-3xl border border-border/60 bg-card/70 backdrop-blur-xl p-5">
            <p className="text-xs uppercase tracking-widest text-primary/70">Status</p>
            <p className="text-3xl font-semibold mt-2">{getStatusLabel(agreement.status)}</p>
            <p className="text-muted-foreground text-sm mt-2 whitespace-pre-wrap">
              {agreement.description || "Sem descrição registrada."}
            </p>
            {acceptanceStatus && (
              <div className="mt-4 rounded-2xl border border-border/60 bg-background/60 p-3 text-sm space-y-1">
                <p className="font-semibold text-base">Fluxo de aceite</p>
                <p>
                  Status: <strong>{acceptanceStatus.status}</strong>
                </p>
                <p>
                  Partes:{" "}
                  <strong>
                    {acceptanceStatus.acceptedParties}/{acceptanceStatus.totalParties} aceitos
                  </strong>
                </p>
                <p>
                  Pendentes: <strong>{acceptanceStatus.pendingParties}</strong>
                </p>
                <p>
                  Dias desde o convite: <strong>{acceptanceStatus.daysElapsed}</strong>
                </p>
                <p>
                  Expirou? <strong>{acceptanceStatus.isExpired ? "Sim" : "Não"}</strong>
                </p>
              </div>
            )}
            <div className="mt-4 space-y-2 text-sm">
              <p>
                Valor Total:{" "}
                <strong>{formatCurrency(agreement.totalValue, agreement.currency)}</strong>
              </p>
              <p>
                Milestones:{" "}
                <strong>
                  {agreement.milestones.length} ({agreement.milestones.filter((m) => m.status === 2 || m.status === "Completed").length} completos)
                </strong>
              </p>
              <p>
                Partes: <strong>{agreement.parties.length}</strong>
              </p>
              <p>
                Escrow:{" "}
                <strong>{agreement.escrowAccountId ? "Conectado" : "Não configurado"}</strong>
              </p>
            </div>
            <div className="mt-4 flex flex-wrap gap-3">
              <Button variant="ghost" onClick={() => router.push("/acordos")}>
                Voltar para lista
              </Button>
              {agreement.status === 2 || agreement.status === "Active" ? (
                <Button onClick={handleCompleteAgreement}>Concluir acordo</Button>
              ) : null}
              <div className="relative">
                <Button
                  variant="ghost"
                  onClick={() => setShowMoreMenu(!showMoreMenu)}
                >
                  ⋯ Mais
                </Button>
                {showMoreMenu && (
                  <div className="absolute right-0 mt-2 w-48 bg-background border border-border rounded-lg shadow-lg z-40">
                    <button
                      onClick={() => {
                        setShowMoreMenu(false);
                        setShowDisputeModal(true);
                      }}
                      className="w-full text-left px-4 py-2 hover:bg-accent text-sm"
                    >
                      📋 Abrir disputa
                    </button>
                    <button
                      onClick={() => {
                        setShowMoreMenu(false);
                        setShowCancelModal(true);
                      }}
                      className="w-full text-left px-4 py-2 hover:bg-accent text-sm text-destructive"
                    >
                      ✕ Cancelar acordo
                    </button>
                  </div>
                )}
              </div>
            </div>
          </div>

          <div className="rounded-3xl border border-border/60 bg-card/70 backdrop-blur-xl p-5">
            <h3 className="text-lg font-semibold mb-3">Partes envolvidas</h3>
            <div className="space-y-3 max-h-72 overflow-auto pr-1">
              {agreement.parties.map((party) => (
                <div
                  key={party.partyId}
                  className="rounded-2xl border border-border/60 bg-background/50 px-4 py-3"
                >
                  <div className="flex justify-between gap-4 mb-2">
                    <div>
                      <p className="font-semibold">{party.partyName}</p>
                      <p className="text-xs text-muted-foreground">{party.email ?? "Sem e-mail"}</p>
                    </div>
                    <div className="text-right text-sm">
                      <p>{getRoleLabel(party.role)}</p>
                      <p>{party.splitPercentage}%</p>
                      <p className="text-xs text-muted-foreground">
                        {party.hasAccepted ? "Aceitou" : "Pendente"}
                      </p>
                    </div>
                  </div>
                  {isActive && (
                    <div className="flex gap-2 items-center">
                      {party.stripeAccountId ? (
                        <span className="text-xs bg-green-100 text-green-800 px-2 py-1 rounded-lg">
                          ✓ Stripe Conectado
                        </span>
                      ) : (
                        <Button
                          size="sm"
                          variant="ghost"
                          className="text-xs"
                          onClick={() => {
                            setSelectedPartyForStripe(party.partyId);
                            setShowStripeModal(true);
                          }}
                        >
                          Conectar Stripe
                        </Button>
                      )}
                    </div>
                  )}
                </div>
              ))}
            </div>
            {isDraft && (
              <div className="mt-4 border-t border-border/40 pt-4">
                <Button
                  className="w-full"
                  variant="ghost"
                  onClick={() => setShowAddPartyModal(true)}
                >
                  + Adicionar parte
                </Button>
              </div>
            )}
          </div>

          <div className="rounded-3xl border border-border/60 bg-card/70 backdrop-blur-xl p-5">
            <h3 className="text-lg font-semibold mb-3">Conta Escrow</h3>
            {agreement.escrowAccountId && escrowAccount ? (
              <>
                <p className="text-sm">
                  Saldo:{" "}
                  <strong>{formatCurrency(escrowAccount.balance, escrowAccount.currency)}</strong>
                </p>
                <p className="text-xs text-muted-foreground mb-4">
                  Status: {escrowAccount.status}
                </p>
                <Button
                  variant="ghost"
                  className="w-full mb-3"
                  onClick={() => router.push(`/escrow/${escrowAccount.escrowAccountId}`)}
                >
                  Abrir ledger completo
                </Button>
              </>
            ) : (
              <div className="text-sm text-muted-foreground mb-4">
                Crie a conta escrow para liberar depósitos e payouts com Stripe Connect.
              </div>
            )}
            {!agreement.escrowAccountId && (
              <Button className="w-full" onClick={handleCreateEscrow}>
                Criar conta escrow
              </Button>
            )}
          </div>
        </div>

        <div className="rounded-3xl border border-border/60 bg-card/70 backdrop-blur-xl p-5">
          <h3 className="text-lg font-semibold mb-3">Milestones & Execução</h3>
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
            <div className="lg:col-span-2 space-y-3 max-h-[420px] overflow-auto pr-2">
              {agreement.milestones.map((milestone) => (
                <button
                  key={milestone.milestoneId}
                  onClick={() => setSelectedMilestone(milestone)}
                  className={`w-full text-left rounded-2xl border border-border/60 px-4 py-3 ${
                    selectedMilestone?.milestoneId === milestone.milestoneId
                      ? "bg-primary/10 border-primary/50"
                      : "bg-background/50"
                  }`}
                >
                  <div className="flex items-center justify-between text-sm">
                    <div>
                      <p className="font-semibold">{milestone.description}</p>
                      <p className="text-xs text-muted-foreground">
                        {new Date(milestone.dueDate).toLocaleDateString()} •{" "}
                        {getMilestoneStatusLabel(milestone.status)}
                      </p>
                    </div>
                    <p className="font-semibold">
                      {formatCurrency(milestone.value, milestone.currency)}
                    </p>
                  </div>
                  {milestone.completionNotes && (
                    <p className="text-xs text-muted-foreground mt-2">
                      {milestone.completionNotes}
                    </p>
                  )}
                </button>
              ))}
            </div>
            <div className="space-y-3 text-sm">
              {isDraft && (
                <div className="rounded-2xl border border-border/60 bg-background/50 p-4">
                  <Button
                    className="w-full"
                    variant="ghost"
                    onClick={() => setShowAddMilestoneModal(true)}
                  >
                    + Novo milestone
                  </Button>
                </div>
              )}
              <div className="rounded-2xl border border-border/60 bg-background/50 p-4 space-y-3">
                <p className="text-xs uppercase tracking-widest text-muted-foreground">
                  Operações
                </p>
                <textarea
                  className="w-full rounded-xl border border-border bg-background/60 px-3 py-2 text-sm"
                  placeholder="Notas para conclusão"
                  value={completeNotes}
                  onChange={(e) => setCompleteNotes(e.target.value)}
                />
                <Button
                  className="w-full"
                  variant="ghost"
                  onClick={handleMilestoneComplete}
                  disabled={!selectedMilestone}
                >
                  Marcar como completo
                </Button>
                <NumericInput
                  value={payoutAmount}
                  onValueChange={(value) => setPayoutAmount(value)}
                  placeholder="Valor para payout"
                  className="w-full rounded-xl border border-border bg-background/60 px-3 py-2 text-sm"
                />
                <select
                  className="w-full rounded-xl border border-border bg-background/60 px-3 py-2 text-sm"
                  value={payoutCurrency}
                  onChange={(e) => setPayoutCurrency(e.target.value)}
                >
                  {CURRENCY_OPTIONS.map(({ code, flag, label }) => (
                    <option key={code} value={code}>
                      {flag} {code} – {label}
                    </option>
                  ))}
                </select>
                <Button
                  className="w-full"
                  onClick={handleMilestonePayout}
                  disabled={!selectedMilestone}
                >
                  Solicitar payout
                </Button>
              </div>
              {selectedMilestone && payoutSplits.length > 0 && (
                <div className="rounded-2xl border border-primary/20 bg-primary/5 p-4 mt-4 space-y-3">
                  <p className="text-xs uppercase tracking-widest text-primary font-semibold">
                    📊 Distribuição de Payout
                  </p>
                  <p className="text-sm text-muted-foreground">
                    Total:{" "}
                    <strong>{formatCurrency(payoutBaseAmount, payoutCurrencyDisplay)}</strong> ·
                    Status:{" "}
                    <strong>
                      {payoutTransaction ? getTransactionStatusLabel(payoutTransaction.status) : "Aguardando"}
                    </strong>
                  </p>
                  <div className="space-y-3">
                    {payoutSplits.map((split) => (
                      <div
                        key={split.partyId}
                        className="flex justify-between gap-4 border-t border-border/40 pt-2"
                      >
                        <div>
                          <p className="font-semibold">{split.partyName}</p>
                          <p className="text-xs text-muted-foreground">
                            {split.email ?? "Sem e-mail"}
                          </p>
                        </div>
                        <div className="text-right">
                          <p className="font-semibold">
                            {formatCurrency(split.share, payoutCurrencyDisplay)}
                          </p>
                          <p className="text-xs text-muted-foreground">{split.statusLabel}</p>
                          <p className="text-xs text-muted-foreground">
                            {split.splitPercentage.toFixed(2)}%
                          </p>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>


        {showStripeModal && (
          <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
            <div className="bg-background border border-border rounded-2xl p-6 max-w-md w-full mx-4 space-y-4">
              <h2 className="text-lg font-semibold">Conectar Stripe</h2>
              <input
                type="text"
                className="w-full rounded-xl border border-border bg-background/60 px-3 py-2 text-sm"
                placeholder="Account ID ou código OAuth (acct_...)"
                value={stripeAccountInput}
                onChange={(e) => setStripeAccountInput(e.target.value)}
              />
              <div className="flex gap-3">
                <Button
                  variant="ghost"
                  className="flex-1"
                  onClick={() => {
                    setShowStripeModal(false);
                    setSelectedPartyForStripe(null);
                    setStripeAccountInput("");
                  }}
                >
                  Cancelar
                </Button>
                <Button
                  className="flex-1"
                  onClick={handleConnectStripe}
                >
                  Conectar
                </Button>
              </div>
            </div>
          </div>
        )}

        {showAddPartyModal && (
          <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
            <div className="bg-background border border-border rounded-2xl p-6 max-w-md w-full mx-4 space-y-4 max-h-[90vh] overflow-auto">
              <h2 className="text-lg font-semibold">Adicionar Parte</h2>
              <div className="space-y-4">
                <div>
                  <label className="text-sm font-medium">Nome da Parte</label>
                  <input
                    type="text"
                    className="w-full rounded-xl border border-border bg-background/60 px-3 py-2 text-sm mt-1"
                    placeholder="Nome completo ou empresa"
                    value={partyForm.partyName}
                    onChange={(e) =>
                      setPartyForm({ ...partyForm, partyName: e.target.value })
                    }
                  />
                  {partyErrors.partyName && (
                    <p className="text-xs text-destructive mt-1">{partyErrors.partyName}</p>
                  )}
                </div>
                <div>
                  <label className="text-sm font-medium">E-mail</label>
                  <input
                    type="email"
                    className="w-full rounded-xl border border-border bg-background/60 px-3 py-2 text-sm mt-1"
                    placeholder="email@example.com"
                    value={partyForm.email || ""}
                    onChange={(e) =>
                      setPartyForm({ ...partyForm, email: e.target.value })
                    }
                  />
                </div>
                <div>
                  <label className="text-sm font-medium">Papel (Função)</label>
                  <select
                    className="w-full rounded-xl border border-border bg-background/60 px-3 py-2 text-sm mt-1"
                    value={partyForm.role}
                    onChange={(e) =>
                      setPartyForm({ ...partyForm, role: e.target.value as PartyRole })
                    }
                  >
                    <option value="Seller">Vendedor</option>
                    <option value="Buyer">Comprador</option>
                    <option value="Broker">Intermediário</option>
                    <option value="Agent">Agente</option>
                    <option value="Witness">Testemunha</option>
                  </select>
                </div>
                <div>
                  <label className="text-sm font-medium">Percentual de Split (%)</label>
                  <input
                    type="number"
                    className="w-full rounded-xl border border-border bg-background/60 px-3 py-2 text-sm mt-1"
                    placeholder="0"
                    min="0"
                    max="100"
                    step="0.01"
                    value={partyForm.splitPercentage || ""}
                    onChange={(e) =>
                      setPartyForm({ ...partyForm, splitPercentage: parseFloat(e.target.value) || 0 })
                    }
                  />
                  {partyErrors.splitPercentage && (
                    <p className="text-xs text-destructive mt-1">{partyErrors.splitPercentage}</p>
                  )}
                </div>
              </div>
              <div className="flex gap-3 mt-6">
                <Button
                  variant="ghost"
                  className="flex-1"
                  onClick={() => {
                    setShowAddPartyModal(false);
                    setPartyForm({
                      partyName: "",
                      email: "",
                      splitPercentage: 0,
                      role: "Agent",
                    });
                    setPartyErrors({});
                  }}
                >
                  Cancelar
                </Button>
                <Button
                  className="flex-1"
                  onClick={handleAddParty}
                >
                  Adicionar
                </Button>
              </div>
            </div>
          </div>
        )}

        {showAddMilestoneModal && (
          <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
            <div className="bg-background border border-border rounded-2xl p-6 max-w-md w-full mx-4 space-y-4">
              <h2 className="text-lg font-semibold">Novo Milestone</h2>
              <div className="space-y-4">
                <div>
                  <label className="text-sm font-medium">Descrição</label>
                  <input
                    type="text"
                    className="w-full rounded-xl border border-border bg-background/60 px-3 py-2 text-sm mt-1"
                    placeholder="Descrição do milestone"
                    value={milestoneForm.description}
                    onChange={(e) =>
                      setMilestoneForm({ ...milestoneForm, description: e.target.value })
                    }
                  />
                </div>
                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <label className="text-sm font-medium">Valor</label>
                    <NumericInput
                      value={milestoneForm.value}
                      onValueChange={(value) =>
                        setMilestoneForm({ ...milestoneForm, value: value || 0 })
                      }
                      placeholder="0.00"
                      className="w-full rounded-xl border border-border bg-background/60 px-3 py-2 text-sm mt-1"
                    />
                  </div>
                  <div>
                    <label className="text-sm font-medium">Moeda</label>
                    <select
                      className="w-full rounded-xl border border-border bg-background/60 px-3 py-2 text-sm mt-1"
                      value={milestoneForm.currency}
                      onChange={(e) =>
                        setMilestoneForm({ ...milestoneForm, currency: e.target.value })
                      }
                    >
                  {CURRENCY_OPTIONS.map(({ code, flag }) => (
                    <option key={code} value={code}>
                      {flag} {code}
                    </option>
                  ))}
                    </select>
                  </div>
                </div>
                <div>
                  <label className="text-sm font-medium">Data de Vencimento</label>
                  <input
                    type="date"
                    className="w-full rounded-xl border border-border bg-background/60 px-3 py-2 text-sm mt-1"
                    value={milestoneForm.dueDate}
                    onChange={(e) =>
                      setMilestoneForm({ ...milestoneForm, dueDate: e.target.value })
                    }
                  />
                </div>
              </div>
              <div className="flex gap-3 mt-6">
                <Button
                  variant="ghost"
                  className="flex-1"
                  onClick={() => {
                    setShowAddMilestoneModal(false);
                    setMilestoneForm({
                      description: "",
                      value: 0,
                      currency: agreement?.currency ?? "BRL",
                      dueDate: "",
                    });
                  }}
                >
                  Cancelar
                </Button>
                <Button
                  className="flex-1"
                  onClick={handleAddMilestone}
                >
                  Adicionar
                </Button>
              </div>
            </div>
          </div>
        )}

        {showDisputeModal && (
          <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
            <div className="bg-background border border-border rounded-2xl p-6 max-w-md w-full mx-4 space-y-4">
              <h2 className="text-lg font-semibold">Abrir Disputa</h2>
              <p className="text-sm text-muted-foreground">
                Descreva o motivo da disputa para que a outra parte seja notificada.
              </p>
              <textarea
                className="w-full rounded-xl border border-border bg-background/60 px-3 py-2 text-sm min-h-[100px]"
                placeholder="Motivo da disputa"
                value={disputeReason}
                onChange={(e) => setDisputeReason(e.target.value)}
              />
              <div className="flex gap-3 mt-6">
                <Button
                  variant="ghost"
                  className="flex-1"
                  onClick={() => {
                    setShowDisputeModal(false);
                    setDisputeReason("");
                  }}
                >
                  Cancelar
                </Button>
                <Button
                  className="flex-1"
                  onClick={() => {
                    handleDispute();
                    setShowDisputeModal(false);
                  }}
                >
                  Abrir Disputa
                </Button>
              </div>
            </div>
          </div>
        )}

        {showCancelModal && (
          <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
            <div className="bg-background border border-border rounded-2xl p-6 max-w-md w-full mx-4 space-y-4">
              <h2 className="text-lg font-semibold text-destructive">Cancelar Acordo</h2>
              <p className="text-sm text-muted-foreground">
                Esta ação é irreversível. Descreva o motivo do cancelamento.
              </p>
              <textarea
                className="w-full rounded-xl border border-border bg-background/60 px-3 py-2 text-sm min-h-[100px]"
                placeholder="Motivo do cancelamento"
                value={cancelReason}
                onChange={(e) => setCancelReason(e.target.value)}
              />
              <div className="flex gap-3 mt-6">
                <Button
                  variant="ghost"
                  className="flex-1"
                  onClick={() => {
                    setShowCancelModal(false);
                    setCancelReason("");
                  }}
                >
                  Voltar
                </Button>
                <Button
                  variant="destructive"
                  className="flex-1"
                  onClick={() => {
                    handleCancel();
                    setShowCancelModal(false);
                  }}
                >
                  Cancelar Acordo
                </Button>
              </div>
            </div>
          </div>
        )}
      </div>
    </LayoutWrapper>
  );
}
