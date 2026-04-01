"use client";

import { useEffect, useMemo, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { LayoutWrapper } from "@/components/LayoutWrapper";
import { Button } from "@/components/ui/button";
import {
  addAgreementPartyClient,
  cancelAgreementClient,
  completeAgreementClient,
  createAgreementMilestoneClient,
  disputeAgreementClient,
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
  AgreementWizardMilestoneInput,
  AgreementWizardPartyInput,
  CommissionAgreementDto,
  MilestoneDto,
} from "@/lib/types/agreements";
import type { EscrowAccountDto } from "@/lib/types/escrow";
import { getMilestoneStatusLabel, getRoleLabel, getStatusLabel } from "@/lib/types/agreements";
import { ApiError, extractApiFieldErrors } from "@/lib/api/types";

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
  const [selectedMilestone, setSelectedMilestone] = useState<MilestoneDto | null>(null);
  const [completeNotes, setCompleteNotes] = useState("");
  const [disputeReason, setDisputeReason] = useState("");
  const [cancelReason, setCancelReason] = useState("");
  const [payoutAmount, setPayoutAmount] = useState("");
  const [payoutCurrency, setPayoutCurrency] = useState("BRL");
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => {
    if (!agreementId) return;
    loadAgreement();
  }, [agreementId]);

  async function loadAgreement() {
    setLoading(true);
    setError(null);
    if (!agreementId) return;
    try {
      const data = await getCommissionAgreementClient(agreementId);
      setAgreement(data);
      setMilestoneForm((prev) => ({ ...prev, currency: data.currency }));
      if (data.escrowAccountId) {
        const escrow = await getEscrowAccountClient(data.escrowAccountId);
        setEscrowAccount(escrow);
      } else {
        setEscrowAccount(null);
      }
    } catch (err) {
      console.error(err);
      setError(err instanceof Error ? err.message : "Erro ao carregar o acordo.");
    } finally {
      setLoading(false);
    }
  }

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
        amount: payoutAmount ? Number(payoutAmount) : undefined,
        currency: payoutCurrency || agreement.currency,
      });
      setPayoutAmount("");
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
            </div>
          </div>

          <div className="rounded-3xl border border-border/60 bg-card/70 backdrop-blur-xl p-5">
            <h3 className="text-lg font-semibold mb-3">Partes envolvidas</h3>
            <div className="space-y-3 max-h-72 overflow-auto pr-1">
              {agreement.parties.map((party) => (
                <div
                  key={party.partyId}
                  className="rounded-2xl border border-border/60 bg-background/50 px-4 py-3 flex justify-between gap-4"
                >
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
              ))}
            </div>
            <div className="mt-4 border-t border-border/40 pt-4 space-y-2">
              <p className="text-xs uppercase tracking-widest text-muted-foreground">Adicionar</p>
              <input
                className={`w-full rounded-xl border bg-background/60 px-3 py-2 text-sm ${
                  partyErrors.partyName ? "border-red-300" : "border-border"
                }`}
                placeholder="Nome da parte"
                value={partyForm.partyName}
                onChange={(e) => setPartyForm((prev) => ({ ...prev, partyName: e.target.value }))}
              />
              {partyErrors.partyName && (
                <p className="text-xs text-red-500">{partyErrors.partyName}</p>
              )}
              <input
                className={`w-full rounded-xl border bg-background/60 px-3 py-2 text-sm ${
                  partyErrors.email ? "border-red-300" : "border-border"
                }`}
                placeholder="Email"
                value={partyForm.email}
                onChange={(e) => setPartyForm((prev) => ({ ...prev, email: e.target.value }))}
              />
              {partyErrors.email && (
                <p className="text-xs text-red-500">{partyErrors.email}</p>
              )}
              <div className="flex gap-3">
                <input
                  type="number"
                  className={`flex-1 rounded-xl border bg-background/60 px-3 py-2 text-sm ${
                    partyErrors.splitPercentage ? "border-red-300" : "border-border"
                  }`}
                  placeholder="%"
                  value={partyForm.splitPercentage}
                  onChange={(e) =>
                    setPartyForm((prev) => ({ ...prev, splitPercentage: Number(e.target.value) }))
                  }
                />
                <select
                  className={`flex-1 rounded-xl border bg-background/60 px-3 py-2 text-sm ${
                    partyErrors.role ? "border-red-300" : "border-border"
                  }`}
                  value={partyForm.role}
                  onChange={(e) =>
                    setPartyForm((prev) => ({ ...prev, role: e.target.value as any }))
                  }
                >
                  <option value="Seller">Seller</option>
                  <option value="Buyer">Buyer</option>
                  <option value="Broker">Broker</option>
                  <option value="Agent">Agent</option>
                  <option value="Witness">Witness</option>
                </select>
              </div>
              {partyErrors.splitPercentage && (
                <p className="text-xs text-red-500">{partyErrors.splitPercentage}</p>
              )}
              {partyErrors.role && (
                <p className="text-xs text-red-500">{partyErrors.role}</p>
              )}
              <Button className="w-full" onClick={handleAddParty}>
                Adicionar parte
              </Button>
            </div>
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
              <div className="rounded-2xl border border-border/60 bg-background/50 p-4 space-y-3">
                <p className="text-xs uppercase tracking-widest text-muted-foreground">
                  Novo milestone
                </p>
                <input
                  className="w-full rounded-xl border border-border bg-background/60 px-3 py-2 text-sm"
                  placeholder="Descrição"
                  value={milestoneForm.description}
                  onChange={(e) =>
                    setMilestoneForm((prev) => ({ ...prev, description: e.target.value }))
                  }
                />
                <input
                  type="number"
                  className="w-full rounded-xl border border-border bg-background/60 px-3 py-2 text-sm"
                  placeholder="Valor"
                  value={milestoneForm.value}
                  onChange={(e) =>
                    setMilestoneForm((prev) => ({ ...prev, value: Number(e.target.value) }))
                  }
                />
                <input
                  type="date"
                  className="w-full rounded-xl border border-border bg-background/60 px-3 py-2 text-sm"
                  value={milestoneForm.dueDate}
                  onChange={(e) =>
                    setMilestoneForm((prev) => ({ ...prev, dueDate: e.target.value }))
                  }
                />
                <Button className="w-full" onClick={handleAddMilestone}>
                  Salvar milestone
                </Button>
              </div>
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
                <input
                  type="number"
                  className="w-full rounded-xl border border-border bg-background/60 px-3 py-2 text-sm"
                  placeholder="Valor para payout"
                  value={payoutAmount}
                  onChange={(e) => setPayoutAmount(e.target.value)}
                />
                <input
                  className="w-full rounded-xl border border-border bg-background/60 px-3 py-2 text-sm uppercase"
                  placeholder="Moeda"
                  value={payoutCurrency}
                  onChange={(e) => setPayoutCurrency(e.target.value.toUpperCase())}
                />
                <Button
                  className="w-full"
                  onClick={handleMilestonePayout}
                  disabled={!selectedMilestone}
                >
                  Solicitar payout
                </Button>
              </div>
            </div>
          </div>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
          <div className="rounded-3xl border border-border/60 bg-card/70 backdrop-blur-xl p-5 space-y-3">
            <h3 className="text-lg font-semibold">Disputa</h3>
            <textarea
              className="w-full rounded-xl border border-border bg-background/60 px-3 py-2 text-sm min-h-[80px]"
              placeholder="Motivo da disputa"
              value={disputeReason}
              onChange={(e) => setDisputeReason(e.target.value)}
            />
            <Button variant="ghost" onClick={handleDispute}>
              Abrir disputa
            </Button>
          </div>
          <div className="rounded-3xl border border-border/60 bg-card/70 backdrop-blur-xl p-5 space-y-3">
            <h3 className="text-lg font-semibold">Cancelamento</h3>
            <textarea
              className="w-full rounded-xl border border-border bg-background/60 px-3 py-2 text-sm min-h-[80px]"
              placeholder="Motivo do cancelamento"
              value={cancelReason}
              onChange={(e) => setCancelReason(e.target.value)}
            />
            <Button variant="destructive" onClick={handleCancel}>
              Cancelar acordo
            </Button>
          </div>
        </div>
      </div>
    </LayoutWrapper>
  );
}
