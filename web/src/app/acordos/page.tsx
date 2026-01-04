"use client";

import { useEffect, useMemo, useState } from "react";
import { useLocale } from "next-intl";
import { useRouter } from "next/navigation";
import { LayoutWrapper } from "@/components/LayoutWrapper";
import { Button } from "@/components/ui/button";
import {
  addAgreementPartyClient,
  createAgreementMilestoneClient,
  createCommissionAgreementClient,
  listCommissionAgreementsClient,
  activateAgreementClient,
} from "@/lib/api/agreementsApiClient";
import { getRoleLabel, getStatusLabel } from "@/lib/types/agreements";
import type {
  AgreementWizardMilestoneInput,
  AgreementWizardPartyInput,
  CommissionAgreementDto,
  PartyRole,
} from "@/lib/types/agreements";
import { cn } from "@/lib/utils";
import NumericInput from "@/components/NumericInput";

const wizardSteps = ["Detalhes", "Partes", "Milestones", "Resumo"] as const;

type CreateCommissionAgreementResponse =
  | { agreementId: string }
  | { sessionId?: string; id?: string };

type WizardStep = (typeof wizardSteps)[number];

interface WizardState {
  title: string;
  description: string;
  terms: string;
  totalValue?: number;
  currency: string;
  parties: AgreementWizardPartyInput[];
  milestones: AgreementWizardMilestoneInput[];
}

const defaultWizardState: WizardState = {
  title: "",
  description: "",
  terms: "",
  totalValue: undefined,
  currency: "BRL",
  parties: [],
  milestones: [],
};

type NewPartyForm = Omit<AgreementWizardPartyInput, "splitPercentage"> & { splitPercentage?: number };
type NewMilestoneForm = Omit<AgreementWizardMilestoneInput, "value"> & { value?: number };

const currencyOptions = [
  { code: "BRL", label: "🇧🇷 BRL (R$)" },
  { code: "USD", label: "🇺🇸 USD ($)" },
  { code: "EUR", label: "🇪🇺 EUR (€)" },
  { code: "GBP", label: "🇬🇧 GBP (£)" },
  { code: "JPY", label: "🇯🇵 JPY (¥)" },
  { code: "CAD", label: "🇨🇦 CAD ($)" },
  { code: "AUD", label: "🇦🇺 AUD ($)" },
  { code: "CHF", label: "🇨🇭 CHF (CHF)" },
];

export default function CommissionAgreementsPage() {
  const locale = useLocale();
  const router = useRouter();
  const [agreements, setAgreements] = useState<CommissionAgreementDto[]>([]);
  const [loadingList, setLoadingList] = useState(true);
  const [loadingWizard, setLoadingWizard] = useState(false);
  const [wizardStepIndex, setWizardStepIndex] = useState(0);
  const [wizardState, setWizardState] = useState<WizardState>(defaultWizardState);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const [newParty, setNewParty] = useState<NewPartyForm>({
    partyName: "",
    email: "",
    splitPercentage: undefined,
    role: "Agent",
  });

  const [newMilestone, setNewMilestone] = useState<NewMilestoneForm>({
    description: "",
    value: undefined,
    currency: "BRL",
    dueDate: "",
  });

  const currentStep: WizardStep = wizardSteps[wizardStepIndex];

  useEffect(() => {
    loadAgreements();
  }, []);

  async function loadAgreements() {
    setLoadingList(true);
    try {
      const data = await listCommissionAgreementsClient();
      setAgreements(data);
    } catch (err) {
      console.error(err);
      setError(
        err instanceof Error ? err.message : "Não foi possível carregar os acordos de comissão.",
      );
    } finally {
      setLoadingList(false);
    }
  }

  const totalSplit = useMemo(
    () => wizardState.parties.reduce((acc, item) => acc + (item.splitPercentage || 0), 0),
    [wizardState.parties],
  );

  const milestonesTotal = useMemo(
    () => wizardState.milestones.reduce((acc, item) => acc + (item.value || 0), 0),
    [wizardState.milestones],
  );

  function handleNextStep() {
    if (wizardStepIndex < wizardSteps.length - 1) {
      setWizardStepIndex((prev) => prev + 1);
    }
  }

  function handlePreviousStep() {
    if (wizardStepIndex > 0) {
      setWizardStepIndex((prev) => prev - 1);
    }
  }

  function addParty() {
    const splitPercentage = newParty.splitPercentage ?? 0;
    if (!newParty.partyName || !newParty.email || splitPercentage <= 0 || splitPercentage > 100) {
      setError("Informe nome, e-mail e percentual válido (maior que 0 e no máximo 100).");
      return;
    }
    const partyToAdd: AgreementWizardPartyInput = {
      ...newParty,
      splitPercentage,
      partyId: crypto.randomUUID(),
    };
    setWizardState((prev) => ({
      ...prev,
      parties: [...prev.parties, partyToAdd],
    }));
    setNewParty({
      partyName: "",
      email: "",
      splitPercentage: undefined,
      role: "Agent",
    });
    setError(null);
  }

  function addMilestone() {
    const milestoneValue = newMilestone.value ?? 0;
    if (!newMilestone.description || milestoneValue <= 0 || !newMilestone.dueDate) {
      setError("Informe descrição, valor e data para o milestone.");
      return;
    }
    const milestoneToAdd: AgreementWizardMilestoneInput = {
      ...newMilestone,
      value: milestoneValue,
      currency: newMilestone.currency || wizardState.currency,
      milestoneId: crypto.randomUUID(),
    };
    setWizardState((prev) => ({
      ...prev,
      milestones: [...prev.milestones, milestoneToAdd],
    }));
    setNewMilestone({
      description: "",
      value: undefined,
      currency: wizardState.currency,
      dueDate: "",
    });
    setError(null);
  }

  function removeParty(partyId: string) {
    setWizardState((prev) => ({
      ...prev,
      parties: prev.parties.filter((p) => p.partyId !== partyId),
    }));
  }

  function removeMilestone(milestoneId: string | undefined) {
    setWizardState((prev) => ({
      ...prev,
      milestones: prev.milestones.filter((m) => m.milestoneId !== milestoneId),
    }));
  }

  async function handleCreateWizard() {
    setError(null);
    setSuccessMessage(null);

    const enteredTotalValue = wizardState.totalValue ?? 0;

    if (!wizardState.title || enteredTotalValue <= 0) {
      setError("Título e valor total são obrigatórios.");
      return;
    }

    if (wizardState.parties.length === 0 || wizardState.milestones.length === 0) {
      setError("Inclua pelo menos uma parte e um milestone antes de criar o acordo.");
      return;
    }

    if (Math.round(totalSplit) !== 100) {
      setError("A soma dos percentuais das partes deve totalizar 100%.");
      return;
    }

    if (Math.round(milestonesTotal) !== Math.round(enteredTotalValue)) {
      setError("A soma dos milestones deve fechar com o valor total do acordo.");
      return;
    }

    setLoadingWizard(true);
    try {
      const response = (await createCommissionAgreementClient({
        title: wizardState.title,
        description: wizardState.description,
        terms: wizardState.terms,
        totalValue: enteredTotalValue,
        currency: wizardState.currency,
      })) as CreateCommissionAgreementResponse;
      const agreementId =
        "agreementId" in response ? response.agreementId : response.id ?? "";

      if (!agreementId) {
        throw new Error("Erro ao criar acordo.");
      }

      for (const party of wizardState.parties) {
        await addAgreementPartyClient(agreementId, party);
      }

      for (const milestone of wizardState.milestones) {
        await createAgreementMilestoneClient(agreementId, milestone);
      }

      await activateAgreementClient(agreementId);

      setSuccessMessage("Acordo criado com sucesso!");
      setWizardState(defaultWizardState);
      setWizardStepIndex(0);
      await loadAgreements();
    } catch (err) {
      console.error(err);
      setError(err instanceof Error ? err.message : "Falha ao criar o acordo.");
    } finally {
      setLoadingWizard(false);
    }
  }

  const formatCurrency = (value: number | undefined, currency: string) => {
    if (value === undefined) {
      return "-";
    }
    try {
    return new Intl.NumberFormat(locale, {
      style: "currency",
      currency,
    }).format(value);
    } catch {
      return `${currency} ${value.toFixed(2)}`;
    }
  };


  async function handleActivateAgreement(agreementId: string) {
    try {
      await activateAgreementClient(agreementId);
      await loadAgreements();
    } catch (err) {
      console.error(err);
      setError(err instanceof Error ? err.message : "Erro ao ativar o acordo.");
    }
  }

  return (
    <LayoutWrapper
      title="Acordos & Escrow"
      subtitle="Crie acordos com múltiplas partes, milestones e fluxo financeiro seguro."
      activeTab="agreements"
    >
      <div className="grid grid-cols-1 xl:grid-cols-2 gap-8">
        <section className="bg-card/70 backdrop-blur-xl border border-border/50 rounded-3xl p-6 shadow-xl shadow-primary/5">
          <div className="flex items-center justify-between mb-6">
            <div>
              <p className="text-xs uppercase tracking-widest text-primary/80 font-semibold">
                Wizard
              </p>
              <h2 className="text-2xl font-semibold">Novo acordo de comissão</h2>
              <p className="text-sm text-muted-foreground">
                Estruture o acordo, defina participantes e milestones antes de ativar.
              </p>
            </div>
            <div className="flex items-center gap-2 text-sm">
              {wizardSteps.map((step, index) => (
                <div key={step} className="flex items-center gap-2">
                  <button
                    className={cn(
                      "w-8 h-8 rounded-full border flex items-center justify-center text-xs font-semibold",
                      index === wizardStepIndex
                        ? "border-primary text-primary"
                        : "border-border text-muted-foreground",
                    )}
                    onClick={() => setWizardStepIndex(index)}
                    type="button"
                  >
                    {index + 1}
                  </button>
                  {index < wizardSteps.length - 1 && (
                    <div className="w-8 h-px bg-border opacity-70" />
                  )}
                </div>
              ))}
            </div>
          </div>

          {error && (
            <div className="mb-4 rounded-xl border border-red-200 bg-red-50/80 text-red-700 px-4 py-3 text-sm">
              {error}
            </div>
          )}
          {successMessage && (
            <div className="mb-4 rounded-xl border border-emerald-200 bg-emerald-50/80 text-emerald-700 px-4 py-3 text-sm">
              {successMessage}
            </div>
          )}

          <div className="space-y-6">
            {currentStep === "Detalhes" && (
              <div className="space-y-4">
                <div>
                  <label className="text-sm font-medium text-muted-foreground">Título</label>
                  <input
                    className="mt-1 w-full rounded-xl border border-border bg-background/60 px-3 py-2"
                    value={wizardState.title}
                    onChange={(e) => setWizardState({ ...wizardState, title: e.target.value })}
                    placeholder="Ex: Comissão de venda HQ Norte"
                  />
                </div>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div>
                    <label className="text-sm font-medium text-muted-foreground">
                      Valor total
                    </label>
                    <NumericInput
                      value={wizardState.totalValue}
                      onValueChange={(val) =>
                        setWizardState((prev) => ({ ...prev, totalValue: val }))
                      }
                      className="mt-1 w-full rounded-xl border border-border bg-background/60 px-3 py-2"
                    />
                  </div>
                  <div>
                    <label className="text-sm font-medium text-muted-foreground">Moeda</label>
                    <select
                      className="mt-1 w-full rounded-xl border border-border bg-background/60 px-3 py-2"
                      value={wizardState.currency}
                      onChange={(e) =>
                        setWizardState({ ...wizardState, currency: e.target.value.toUpperCase() })
                      }
                    >
                      {currencyOptions.map((currency) => (
                        <option key={currency.code} value={currency.code}>
                          {currency.label}
                        </option>
                      ))}
                    </select>
                  </div>
                </div>
                <div>
                  <label className="text-sm font-medium text-muted-foreground">
                    Descrição / escopo
                  </label>
                  <textarea
                    className="mt-1 w-full rounded-xl border border-border bg-background/60 px-3 py-2 min-h-[100px]"
                    value={wizardState.description}
                    onChange={(e) =>
                      setWizardState({ ...wizardState, description: e.target.value })
                    }
                    placeholder="Contextualize o acordo, entregáveis, prazos etc."
                  />
                </div>
                <div>
                  <label className="text-sm font-medium text-muted-foreground">Termos</label>
                  <textarea
                    className="mt-1 w-full rounded-xl border border-border bg-background/60 px-3 py-2 min-h-[80px]"
                    value={wizardState.terms}
                    onChange={(e) => setWizardState({ ...wizardState, terms: e.target.value })}
                    placeholder="Condições especiais, cláusulas, observações..."
                  />
                </div>
              </div>
            )}

            {currentStep === "Partes" && (
              <div className="space-y-6">
                <div className="rounded-2xl border border-border/70 bg-background/60 p-4">
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div>
                      <label className="text-sm font-medium text-muted-foreground">
                        Nome da parte
                      </label>
                      <input
                        className="mt-1 w-full rounded-xl border border-border bg-background/60 px-3 py-2"
                        value={newParty.partyName}
                        onChange={(e) =>
                          setNewParty((prev) => ({ ...prev, partyName: e.target.value }))
                        }
                      />
                    </div>
                    <div>
                      <label className="text-sm font-medium text-muted-foreground">E-mail</label>
                      <input
                        className="mt-1 w-full rounded-xl border border-border bg-background/60 px-3 py-2"
                        value={newParty.email}
                        onChange={(e) =>
                          setNewParty((prev) => ({ ...prev, email: e.target.value }))
                        }
                        required
                      />
                    </div>
                    <div>
                      <label className="text-sm font-medium text-muted-foreground">
                        Percentual (%)
                      </label>
                          <NumericInput
                            value={newParty.splitPercentage}
                            onValueChange={(val) =>
                              setNewParty((prev) => ({ ...prev, splitPercentage: val }))
                            }
                            className="mt-1 w-full rounded-xl border border-border bg-background/60 px-3 py-2"
                            decimalScale={2}
                            placeholder="0.00"
                          />
                    </div>
                    <div>
                      <label className="text-sm font-medium text-muted-foreground">Função</label>
                      <select
                        className="mt-1 w-full rounded-xl border border-border bg-background/60 px-3 py-2"
                        value={newParty.role}
                        onChange={(e) =>
                          setNewParty((prev) => ({ ...prev, role: e.target.value as PartyRole }))
                        }
                      >
                        <option value="Seller">Seller</option>
                        <option value="Buyer">Buyer</option>
                        <option value="Broker">Broker</option>
                        <option value="Agent">Agent</option>
                        <option value="Witness">Witness</option>
                      </select>
                    </div>
                  </div>
                  <div className="mt-4 flex justify-end">
                    <Button type="button" onClick={addParty}>
                      Adicionar parte
                    </Button>
                  </div>
                </div>

                <div className="space-y-2">
                  <div className="flex items-center justify-between text-sm text-muted-foreground">
                    <span>{wizardState.parties.length} partes adicionadas</span>
                    <span>Total: {totalSplit}%</span>
                  </div>
                  <div className="space-y-3">
                    {wizardState.parties.map((party) => (
                      <div
                        key={party.partyId}
                        className="rounded-2xl border border-border/70 bg-background/60 px-4 py-3 flex flex-col md:flex-row md:items-center md:justify-between gap-2"
                      >
                        <div>
                          <p className="font-semibold">{party.partyName}</p>
                          <p className="text-xs text-muted-foreground">{party.email}</p>
                        </div>
                        <div className="flex items-center gap-6 text-sm">
                          <div>
                            <span className="text-muted-foreground block text-xs">Percentual</span>
                            <strong>{party.splitPercentage}%</strong>
                          </div>
                          <div>
                            <span className="text-muted-foreground block text-xs">Função</span>
                            <strong>{getRoleLabel(party.role ?? "Agent")}</strong>
                          </div>
                          <button
                            className="text-xs text-destructive"
                            onClick={() => removeParty(party.partyId!)}
                          >
                            remover
                          </button>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              </div>
            )}

            {currentStep === "Milestones" && (
              <div className="space-y-6">
                <div className="rounded-2xl border border-border/70 bg-background/60 p-4">
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div className="md:col-span-2">
                      <label className="text-sm font-medium text-muted-foreground">
                        Descrição
                      </label>
                      <input
                        className="mt-1 w-full rounded-xl border border-border bg-background/60 px-3 py-2"
                        value={newMilestone.description}
                        onChange={(e) =>
                          setNewMilestone((prev) => ({ ...prev, description: e.target.value }))
                        }
                      />
                    </div>
                    <div>
                      <label className="text-sm font-medium text-muted-foreground">Valor</label>
                          <NumericInput
                            value={newMilestone.value}
                            onValueChange={(val) =>
                              setNewMilestone((prev) => ({ ...prev, value: val }))
                            }
                            className="mt-1 w-full rounded-xl border border-border bg-background/60 px-3 py-2"
                          />
                    </div>
                    <div>
                      <label className="text-sm font-medium text-muted-foreground">Data</label>
                      <input
                        type="date"
                        className="mt-1 w-full rounded-xl border border-border bg-background/60 px-3 py-2"
                        value={newMilestone.dueDate}
                        onChange={(e) =>
                          setNewMilestone((prev) => ({ ...prev, dueDate: e.target.value }))
                        }
                      />
                    </div>
                  </div>
                  <div className="mt-4 flex justify-end">
                    <Button type="button" onClick={addMilestone}>
                      Adicionar milestone
                    </Button>
                  </div>
                </div>

                <div className="space-y-3">
                  {wizardState.milestones.map((milestone) => (
                    <div
                      key={milestone.milestoneId}
                      className="rounded-2xl border border-border/70 bg-background/60 px-4 py-3 flex flex-col md:flex-row md:items-center md:justify-between gap-2"
                    >
                      <div>
                        <p className="font-semibold">{milestone.description}</p>
                        <p className="text-xs text-muted-foreground">
                          {milestone.dueDate
                            ? new Date(milestone.dueDate).toLocaleDateString()
                            : "Sem data"}
                        </p>
                      </div>
                      <div className="flex items-center gap-4 text-sm">
                        <strong>
                          {formatCurrency(milestone.value, milestone.currency ?? wizardState.currency)}
                        </strong>
                        <button
                          className="text-xs text-destructive"
                          onClick={() => removeMilestone(milestone.milestoneId)}
                        >
                          remover
                        </button>
                      </div>
                    </div>
                  ))}
                  <div className="text-xs text-muted-foreground">
                    Soma atual dos milestones:{" "}
                    {formatCurrency(milestonesTotal, wizardState.currency)}
                  </div>
                </div>
              </div>
            )}

            {currentStep === "Resumo" && (
              <div className="space-y-4 text-sm">
                <div className="rounded-2xl border border-border/70 bg-background/60 p-4">
                  <h3 className="text-lg font-semibold mb-2">{wizardState.title || "Sem título"}</h3>
                  <p className="text-muted-foreground whitespace-pre-line">
                    {wizardState.description || "Sem descrição"}
                  </p>
                </div>
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                  <div className="rounded-2xl border border-border/70 bg-background/60 p-4">
                    <p className="text-xs text-muted-foreground uppercase">Valor total</p>
                    <p className="text-2xl font-semibold">
                      {formatCurrency(wizardState.totalValue, wizardState.currency)}
                    </p>
                  </div>
                  <div className="rounded-2xl border border-border/70 bg-background/60 p-4">
                    <p className="text-xs text-muted-foreground uppercase">Partes</p>
                    <p className="text-2xl font-semibold">{wizardState.parties.length}</p>
                    <p className="text-xs text-muted-foreground">Soma {totalSplit}%</p>
                  </div>
                  <div className="rounded-2xl border border-border/70 bg-background/60 p-4">
                    <p className="text-xs text-muted-foreground uppercase">Milestones</p>
                    <p className="text-2xl font-semibold">{wizardState.milestones.length}</p>
                    <p className="text-xs text-muted-foreground">
                      {formatCurrency(milestonesTotal, wizardState.currency)}
                    </p>
                  </div>
                </div>
              </div>
            )}

            <div className="flex items-center justify-between pt-4 border-t border-border/50">
              <div className="text-xs text-muted-foreground">
                Etapa {wizardStepIndex + 1} de {wizardSteps.length}
              </div>
              <div className="flex gap-3">
                <Button type="button" variant="ghost" disabled={wizardStepIndex === 0} onClick={handlePreviousStep}>
                  Voltar
                </Button>
                {wizardStepIndex < wizardSteps.length - 1 ? (
                  <Button type="button" onClick={handleNextStep}>
                    Avançar
                  </Button>
                ) : (
                  <Button type="button" onClick={handleCreateWizard} disabled={loadingWizard}>
                    {loadingWizard ? "Criando..." : "Finalizar e ativar"}
                  </Button>
                )}
              </div>
            </div>
          </div>
        </section>

        <section className="bg-card/70 backdrop-blur-xl border border-border/50 rounded-3xl p-6 shadow-xl shadow-primary/5">
          <div className="flex items-center justify-between mb-6">
            <div>
              <p className="text-xs uppercase tracking-widest text-primary/80 font-semibold">
                Pipeline
              </p>
              <h2 className="text-2xl font-semibold">Acordos em andamento</h2>
              <p className="text-sm text-muted-foreground">
                Visualize o status, responsáveis e progresso financeiro de cada acordo.
              </p>
            </div>
            <Button variant="ghost" onClick={loadAgreements} disabled={loadingList}>
              Atualizar
            </Button>
          </div>

          {loadingList ? (
            <div className="text-sm text-muted-foreground">Carregando acordos...</div>
          ) : agreements.length === 0 ? (
            <div className="rounded-2xl border border-dashed border-border/70 p-6 text-center text-sm text-muted-foreground">
              Nenhum acordo cadastrado ainda. Use o wizard ao lado para criar o primeiro.
            </div>
          ) : (
            <div className="space-y-4">
              {agreements.map((agreement) => (
                <div
                  key={agreement.agreementId}
                  className="rounded-2xl border border-border/70 bg-background/60 p-4 flex flex-col gap-4"
                >
                  <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-2">
                    <div>
                      <h3 className="text-lg font-semibold">{agreement.title}</h3>
                      <p className="text-xs text-muted-foreground">
                        Criado em {new Date(agreement.createdAt).toLocaleDateString()}
                      </p>
                    </div>
                    <div className="flex items-center gap-2 text-xs">
                      <span className="px-3 py-1 rounded-full bg-primary/10 text-primary">
                        {getStatusLabel(agreement.status)}
                      </span>
                      <span className="px-3 py-1 rounded-full bg-border/40 text-muted-foreground">
                        {agreement.parties.length} partes
                      </span>
                      <span className="px-3 py-1 rounded-full bg-border/40 text-muted-foreground">
                        {agreement.milestones.length} milestones
                      </span>
                    </div>
                  </div>
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4 text-sm">
                    <div className="rounded-2xl border border-border/60 p-3">
                      <p className="text-muted-foreground text-xs uppercase">Valor</p>
                      <p className="text-xl font-semibold">
                        {formatCurrency(agreement.totalValue, agreement.currency)}
                      </p>
                    </div>
                    <div className="rounded-2xl border border-border/60 p-3">
                      <p className="text-muted-foreground text-xs uppercase">Escrow</p>
                      <p className="text-base font-semibold">
                        {agreement.escrowAccountId ? "Conectado" : "Pendente"}
                      </p>
                    </div>
                  </div>
                  <div className="flex flex-wrap gap-3">
                    <Button
                      variant="ghost"
                      onClick={() => {
                        if (!agreement.agreementId) {
                          setError("Acordo inválido. Recarregue a lista e tente novamente.");
                          return;
                        }
                        router.push(`/acordos/${agreement.agreementId}`);
                      }}
                    >
                      Abrir
                    </Button>
                    {agreement.status === 1 || agreement.status === "Draft" ? (
                      <Button
                        variant="ghost"
                        onClick={() => {
                          if (!agreement.agreementId) {
                            setError("Acordo inválido. Recarregue a lista e tente novamente.");
                            return;
                          }
                          handleActivateAgreement(agreement.agreementId);
                        }}
                      >
                        Ativar
                      </Button>
                    ) : null}
                  </div>
                </div>
              ))}
            </div>
          )}
        </section>
      </div>
    </LayoutWrapper>
  );
}
