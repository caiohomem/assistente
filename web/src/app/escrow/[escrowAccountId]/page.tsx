"use client";
import { Elements, PaymentElement, useElements, useStripe } from "@stripe/react-stripe-js";
import React, { useCallback, useEffect, useState } from "react";
import type { StripeElementsOptions } from "@stripe/stripe-js";
import { loadStripe } from "@stripe/stripe-js";
import { useParams, useRouter } from "next/navigation";
import { LayoutWrapper } from "@/components/LayoutWrapper";
import { Button } from "@/components/ui/button";
import NumericInput from "@/components/NumericInput";
import {
  approvePayoutClient,
  connectStripeAccountClient,
  depositEscrowClient,
  getEscrowAccountClient,
  listEscrowTransactionsClient,
  rejectPayoutClient,
} from "@/lib/api/escrowApiClient";
import type {
  EscrowAccountDto,
  EscrowDepositResult,
  EscrowTransactionDto,
  EscrowTransactionStatus,
  EscrowTransactionType,
} from "@/lib/types/escrow";

type SortOrder = "newest" | "oldest";

export default function EscrowLedgerPage() {
  const params = useParams();
  const router = useRouter();
  const escrowAccountId = React.useMemo(() => {
    const value = params?.escrowAccountId;
    const resolved = Array.isArray(value) ? value[0] : value;
    if (!resolved || resolved === "undefined" || resolved === "null") {
      return null;
    }
    return resolved;
  }, [params]);

  const [account, setAccount] = useState<EscrowAccountDto | null>(null);
  const [transactions, setTransactions] = useState<EscrowTransactionDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  // Filters
  const [filterType, setFilterType] = useState<string>("all");
  const [filterStatus, setFilterStatus] = useState<string>("all");
  const [sortOrder, setSortOrder] = useState<SortOrder>("newest");

  // Modal
  const [selectedTransaction, setSelectedTransaction] = useState<EscrowTransactionDto | null>(null);
  const [rejectReason, setRejectReason] = useState("");

  // Deposit modal
  const [showDepositModal, setShowDepositModal] = useState(false);
  const [depositAmount, setDepositAmount] = useState<number | undefined>(undefined);
  const [depositDescription, setDepositDescription] = useState("");
  const [depositLoading, setDepositLoading] = useState(false);
  const [depositIntent, setDepositIntent] = useState<EscrowDepositResult | null>(null);
  const [confirmingPayment, setConfirmingPayment] = useState(false);
  const [paymentError, setPaymentError] = useState<string | null>(null);
  const stripePublicKey = process.env.NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY;
  const stripePromise = React.useMemo(
    () => (stripePublicKey ? loadStripe(stripePublicKey) : null),
    [stripePublicKey],
  );
  const stripeElementsOptions = React.useMemo<StripeElementsOptions | undefined>(
    () => (depositIntent ? { clientSecret: depositIntent.clientSecret } : undefined),
    [depositIntent],
  );

  // Stripe Connect
  const [stripeCode, setStripeCode] = useState("");
  const [showStripeModal, setShowStripeModal] = useState(false);

  function resetDepositForm() {
    setDepositAmount(undefined);
    setDepositDescription("");
    setDepositIntent(null);
    setPaymentError(null);
    setConfirmingPayment(false);
    setDepositLoading(false);
  }

  function openDepositModal() {
    resetDepositForm();
    setShowDepositModal(true);
  }

  function closeDepositModal() {
    setShowDepositModal(false);
    resetDepositForm();
  }

  const loadAccount = useCallback(async () => {
    if (!escrowAccountId) return;
    setLoading(true);
    setError(null);
    try {
      const data = await getEscrowAccountClient(escrowAccountId);
      setAccount(data);
      const txs = await listEscrowTransactionsClient(escrowAccountId);
      setTransactions(txs);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Erro ao carregar conta escrow.");
    } finally {
      setLoading(false);
    }
  }, [escrowAccountId]);

  const handleStripeCallback = useCallback(
    async (code: string) => {
      if (!escrowAccountId) return;
      try {
        await connectStripeAccountClient(escrowAccountId, code);
        setMessage("Conta Stripe conectada com sucesso!");
        window.history.replaceState({}, "", window.location.pathname);
        await loadAccount();
      } catch (err) {
        setError(err instanceof Error ? err.message : "Erro ao conectar conta Stripe.");
      }
    },
    [escrowAccountId, loadAccount],
  );

  useEffect(() => {
    if (!escrowAccountId) {
      setError("Conta escrow não encontrada.");
      setLoading(false);
      return;
    }
    loadAccount();
  }, [escrowAccountId, loadAccount]);

  // Check for Stripe OAuth callback
  useEffect(() => {
    if (typeof window !== "undefined") {
      const urlParams = new URLSearchParams(window.location.search);
      const code = urlParams.get("code");
      if (code && escrowAccountId) {
        handleStripeCallback(code);
      }
    }
  }, [escrowAccountId, handleStripeCallback]);

  const filteredTransactions = React.useMemo(() => {
    let result = [...transactions];

    // Filter by type
    if (filterType !== "all") {
      result = result.filter((tx) => {
        const type = typeof tx.type === "number" ? getTypeLabel(tx.type) : tx.type;
        return type === filterType;
      });
    }

    // Filter by status
    if (filterStatus !== "all") {
      result = result.filter((tx) => {
        const status = typeof tx.status === "number" ? getStatusLabel(tx.status) : tx.status;
        return status === filterStatus;
      });
    }

    // Sort
    result.sort((a, b) => {
      const dateA = new Date(a.createdAt).getTime();
      const dateB = new Date(b.createdAt).getTime();
      return sortOrder === "newest" ? dateB - dateA : dateA - dateB;
    });

    return result;
  }, [transactions, filterType, filterStatus, sortOrder]);

  function getTypeLabel(type: EscrowTransactionType): string {
    if (typeof type === "number") {
      const labels: Record<number, string> = { 1: "Deposit", 2: "Payout", 3: "Refund", 4: "Fee" };
      return labels[type] ?? `#${type}`;
    }
    return type;
  }

  function getStatusLabel(status: EscrowTransactionStatus): string {
    if (typeof status === "number") {
      const labels: Record<number, string> = {
        1: "Pending",
        2: "Approved",
        3: "Rejected",
        4: "Disputed",
        5: "Completed",
        6: "Failed",
      };
      return labels[status] ?? `#${status}`;
    }
    return status;
  }

  function getStatusBadgeClass(status: EscrowTransactionStatus): string {
    const s = typeof status === "number" ? getStatusLabel(status) : status;
    switch (s) {
      case "Completed":
        return "bg-emerald-100 text-emerald-700";
      case "Approved":
        return "bg-blue-100 text-blue-700";
      case "Pending":
        return "bg-amber-100 text-amber-700";
      case "Rejected":
      case "Failed":
        return "bg-red-100 text-red-700";
      case "Disputed":
        return "bg-purple-100 text-purple-700";
      default:
        return "bg-gray-100 text-gray-700";
    }
  }

  const formatCurrency = (value: number, currency: string) => {
    try {
      return new Intl.NumberFormat("pt-BR", { style: "currency", currency }).format(value);
    } catch {
      return `${currency} ${value.toFixed(2)}`;
    }
  };

  async function handleApprove(tx: EscrowTransactionDto) {
    if (!escrowAccountId) return;
    try {
      await approvePayoutClient(escrowAccountId, tx.transactionId);
      setMessage("Payout aprovado com sucesso!");
      setSelectedTransaction(null);
      await loadAccount();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Erro ao aprovar payout.");
    }
  }

  async function handleReject(tx: EscrowTransactionDto) {
    if (!escrowAccountId) return;
    if (!rejectReason) {
      setError("Informe o motivo da rejeição.");
      return;
    }
    try {
      await rejectPayoutClient(escrowAccountId, tx.transactionId, rejectReason);
      setMessage("Payout rejeitado.");
      setSelectedTransaction(null);
      setRejectReason("");
      await loadAccount();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Erro ao rejeitar payout.");
    }
  }

  async function handleCreateDepositIntent() {
    if (!escrowAccountId || !account) return;
    const amount = depositAmount ?? 0;
    if (amount <= 0) {
      setError("Informe um valor válido para o depósito.");
      return;
    }
    setDepositLoading(true);
    setPaymentError(null);
    try {
      const result = await depositEscrowClient({
        escrowAccountId,
        amount,
        currency: account.currency,
        description: depositDescription || undefined,
      });
      setDepositIntent(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Erro ao criar depósito.");
    } finally {
      setDepositLoading(false);
    }
  }

  async function handleDepositConfirmed() {
    setMessage("Depósito confirmado e saldo atualizado.");
    await loadAccount();
    closeDepositModal();
  }

  async function handleConnectStripe() {
    if (!stripeCode) {
      setError("Informe o código de autorização do Stripe.");
      return;
    }
    if (!escrowAccountId) return;
    try {
      await connectStripeAccountClient(escrowAccountId, stripeCode);
      setMessage("Conta Stripe conectada com sucesso!");
      setShowStripeModal(false);
      setStripeCode("");
      await loadAccount();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Erro ao conectar conta Stripe.");
    }
  }

  function initiateStripeConnect() {
    // Redirect to Stripe Connect OAuth
    const clientId = process.env.NEXT_PUBLIC_STRIPE_CLIENT_ID;
    if (!clientId) {
      setShowStripeModal(true);
      return;
    }
    const redirectUri = `${window.location.origin}/escrow/${escrowAccountId}`;
    const stripeUrl = `https://connect.stripe.com/oauth/authorize?response_type=code&client_id=${clientId}&scope=read_write&redirect_uri=${encodeURIComponent(redirectUri)}`;
    window.location.href = stripeUrl;
  }

  const isPending = (status: EscrowTransactionStatus) => {
    return status === "Pending" || status === 1;
  };

  const isPayout = (type: EscrowTransactionType) => {
    return type === "Payout" || type === 2;
  };

  return (
    <LayoutWrapper
      title="Ledger do Escrow"
      subtitle="Transparência completa sobre depósitos, payouts e fluxos financeiros."
      activeTab="agreements"
    >
      {error && (
        <div className="rounded-xl border border-red-200 bg-red-50/80 text-red-700 px-4 py-3 text-sm mb-4">
          {error}
          <button className="ml-2 underline" onClick={() => setError(null)}>
            Fechar
          </button>
        </div>
      )}
      {message && (
        <div className="rounded-xl border border-emerald-200 bg-emerald-50/80 text-emerald-700 px-4 py-3 text-sm mb-4">
          {message}
          <button className="ml-2 underline" onClick={() => setMessage(null)}>
            Fechar
          </button>
        </div>
      )}
      {loading && <div className="text-sm text-muted-foreground">Carregando...</div>}
      {!loading && account && (
        <div className="space-y-6">
          {/* Account Info */}
          <div className="rounded-3xl border border-border/60 bg-card/70 backdrop-blur-xl p-5 grid grid-cols-1 md:grid-cols-4 gap-4">
            <div>
              <p className="text-xs uppercase tracking-widest text-muted-foreground">Saldo</p>
              <p className="text-3xl font-semibold">
                {formatCurrency(account.balance, account.currency)}
              </p>
            </div>
            <div>
              <p className="text-xs uppercase tracking-widest text-muted-foreground">Status</p>
              <p className="text-xl font-semibold">{account.status}</p>
            </div>
            <div>
              <p className="text-xs uppercase tracking-widest text-muted-foreground">Stripe</p>
              {account.stripeConnectedAccountId ? (
                <p className="text-sm font-semibold text-emerald-600">
                  {account.stripeConnectedAccountId}
                </p>
              ) : (
                <Button variant="ghost" size="sm" className="mt-1 p-0 h-auto" onClick={initiateStripeConnect}>
                  Conectar Stripe →
                </Button>
              )}
            </div>
            <div className="flex items-end justify-end gap-2 flex-wrap">
              <Button variant="ghost" onClick={() => router.push(`/acordos/${account.agreementId}`)}>
                Voltar
              </Button>
              <Button onClick={openDepositModal}>Depositar</Button>
            </div>
          </div>

          {/* Filters */}
          <div className="flex flex-wrap gap-3 items-center">
            <select
              className="rounded-xl border border-border bg-background/60 px-3 py-2 text-sm"
              value={filterType}
              onChange={(e) => setFilterType(e.target.value)}
            >
              <option value="all">Todos os tipos</option>
              <option value="Deposit">Depósito</option>
              <option value="Payout">Payout</option>
              <option value="Refund">Reembolso</option>
              <option value="Fee">Taxa</option>
            </select>
            <select
              className="rounded-xl border border-border bg-background/60 px-3 py-2 text-sm"
              value={filterStatus}
              onChange={(e) => setFilterStatus(e.target.value)}
            >
              <option value="all">Todos os status</option>
              <option value="Pending">Pendente</option>
              <option value="Approved">Aprovado</option>
              <option value="Completed">Completo</option>
              <option value="Rejected">Rejeitado</option>
              <option value="Failed">Falhou</option>
              <option value="Disputed">Em disputa</option>
            </select>
            <select
              className="rounded-xl border border-border bg-background/60 px-3 py-2 text-sm"
              value={sortOrder}
              onChange={(e) => setSortOrder(e.target.value as SortOrder)}
            >
              <option value="newest">Mais recentes</option>
              <option value="oldest">Mais antigas</option>
            </select>
            <Button variant="ghost" onClick={loadAccount}>
              Atualizar
            </Button>
          </div>

          {/* Transactions Table */}
          <div className="rounded-3xl border border-border/60 bg-card/70 backdrop-blur-xl p-5">
            <div className="mb-4">
              <p className="text-xs uppercase tracking-widest text-primary/70">Ledger</p>
              <h3 className="text-lg font-semibold">
                Transações ({filteredTransactions.length})
              </h3>
            </div>
            <div className="overflow-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="text-xs uppercase text-muted-foreground border-b border-border/50">
                    <th className="py-2 text-left">Data</th>
                    <th className="py-2 text-left">Tipo</th>
                    <th className="py-2 text-left">Status</th>
                    <th className="py-2 text-left">Valor</th>
                    <th className="py-2 text-left">Descrição</th>
                    <th className="py-2 text-left">Ações</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredTransactions.length === 0 && (
                    <tr>
                      <td className="py-4 text-muted-foreground" colSpan={6}>
                        Nenhuma transação encontrada.
                      </td>
                    </tr>
                  )}
                  {filteredTransactions.map((tx) => (
                    <tr
                      key={tx.transactionId}
                      className="border-b border-border/40 hover:bg-background/30 cursor-pointer"
                      onClick={() => setSelectedTransaction(tx)}
                    >
                      <td className="py-2">
                        {new Date(tx.createdAt).toLocaleString("pt-BR", {
                          dateStyle: "short",
                          timeStyle: "short",
                        })}
                      </td>
                      <td className="py-2">{getTypeLabel(tx.type)}</td>
                      <td className="py-2">
                        <span
                          className={`px-2 py-0.5 rounded-full text-xs font-medium ${getStatusBadgeClass(tx.status)}`}
                        >
                          {getStatusLabel(tx.status)}
                        </span>
                      </td>
                      <td className="py-2 font-semibold">
                        {formatCurrency(tx.amount, tx.currency)}
                      </td>
                      <td className="py-2 text-muted-foreground max-w-[200px] truncate">
                        {tx.description ?? "-"}
                      </td>
                      <td className="py-2">
                        {isPending(tx.status) && isPayout(tx.type) && (
                          <div className="flex gap-2" onClick={(e) => e.stopPropagation()}>
                            <Button
                              size="sm"
                              variant="ghost"
                              className="text-emerald-600 h-7 px-2"
                              onClick={() => handleApprove(tx)}
                            >
                              Aprovar
                            </Button>
                            <Button
                              size="sm"
                              variant="ghost"
                              className="text-red-600 h-7 px-2"
                              onClick={() => {
                                setSelectedTransaction(tx);
                              }}
                            >
                              Rejeitar
                            </Button>
                          </div>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}

      {/* Transaction Detail Modal */}
      {selectedTransaction && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-card rounded-3xl border border-border/60 p-6 max-w-lg w-full max-h-[90vh] overflow-auto">
            <div className="flex justify-between items-start mb-4">
              <h3 className="text-lg font-semibold">Detalhes da Transação</h3>
              <button
                className="text-muted-foreground hover:text-foreground"
                onClick={() => {
                  setSelectedTransaction(null);
                  setRejectReason("");
                }}
              >
                ✕
              </button>
            </div>
            <div className="space-y-3 text-sm">
              <div className="flex justify-between">
                <span className="text-muted-foreground">ID:</span>
                <span className="font-mono text-xs">{selectedTransaction.transactionId}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Tipo:</span>
                <span>{getTypeLabel(selectedTransaction.type)}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Status:</span>
                <span
                  className={`px-2 py-0.5 rounded-full text-xs font-medium ${getStatusBadgeClass(selectedTransaction.status)}`}
                >
                  {getStatusLabel(selectedTransaction.status)}
                </span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Valor:</span>
                <span className="font-semibold">
                  {formatCurrency(selectedTransaction.amount, selectedTransaction.currency)}
                </span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Criado em:</span>
                <span>
                  {new Date(selectedTransaction.createdAt).toLocaleString("pt-BR")}
                </span>
              </div>
              {selectedTransaction.description && (
                <div>
                  <span className="text-muted-foreground">Descrição:</span>
                  <p className="mt-1">{selectedTransaction.description}</p>
                </div>
              )}
              {selectedTransaction.stripePaymentIntentId && (
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Payment Intent:</span>
                  <span className="font-mono text-xs">{selectedTransaction.stripePaymentIntentId}</span>
                </div>
              )}
              {selectedTransaction.stripeTransferId && (
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Transfer ID:</span>
                  <span className="font-mono text-xs">{selectedTransaction.stripeTransferId}</span>
                </div>
              )}
              {selectedTransaction.approvedBy && (
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Aprovado por:</span>
                  <span className="font-mono text-xs">{selectedTransaction.approvedBy}</span>
                </div>
              )}
              {selectedTransaction.rejectionReason && (
                <div>
                  <span className="text-muted-foreground">Motivo rejeição:</span>
                  <p className="mt-1 text-red-600">{selectedTransaction.rejectionReason}</p>
                </div>
              )}
              {selectedTransaction.disputeReason && (
                <div>
                  <span className="text-muted-foreground">Motivo disputa:</span>
                  <p className="mt-1 text-purple-600">{selectedTransaction.disputeReason}</p>
                </div>
              )}
            </div>

            {/* Reject form */}
            {isPending(selectedTransaction.status) && isPayout(selectedTransaction.type) && (
              <div className="mt-6 pt-4 border-t border-border/40 space-y-3">
                <p className="text-sm font-medium">Ações</p>
                <div className="flex gap-2">
                  <Button
                    className="flex-1"
                    onClick={() => handleApprove(selectedTransaction)}
                  >
                    Aprovar Payout
                  </Button>
                </div>
                <textarea
                  className="w-full rounded-xl border border-border bg-background/60 px-3 py-2 text-sm"
                  placeholder="Motivo da rejeição"
                  value={rejectReason}
                  onChange={(e) => setRejectReason(e.target.value)}
                />
                <Button
                  variant="destructive"
                  className="w-full"
                  onClick={() => handleReject(selectedTransaction)}
                >
                  Rejeitar Payout
                </Button>
              </div>
            )}

            <div className="mt-4 flex justify-end">
              <Button
                variant="ghost"
                onClick={() => {
                  setSelectedTransaction(null);
                  setRejectReason("");
                }}
              >
                Fechar
              </Button>
            </div>
          </div>
        </div>
      )}

      {/* Deposit Modal */}
      {showDepositModal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-card rounded-3xl border border-border/60 p-6 max-w-md w-full">
            <div className="flex justify-between items-start mb-4">
              <h3 className="text-lg font-semibold">Depositar Fundos</h3>
              <button
                className="text-muted-foreground hover:text-foreground"
                onClick={closeDepositModal}
              >
                ✕
              </button>
            </div>
            <div className="space-y-4">
              {!depositIntent ? (
                <>
                  <div>
                    <label className="text-sm text-muted-foreground">Valor ({account?.currency})</label>
                    <NumericInput
                      value={depositAmount}
                      onValueChange={(val) => setDepositAmount(val)}
                      className="w-full mt-1 rounded-xl border border-border bg-background/60 px-3 py-2 text-sm"
                      placeholder="0.00"
                    />
                  </div>
                  <div>
                    <label className="text-sm text-muted-foreground">Descrição (opcional)</label>
                    <input
                      className="w-full mt-1 rounded-xl border border-border bg-background/60 px-3 py-2 text-sm"
                      placeholder="Descrição do depósito"
                      value={depositDescription}
                      onChange={(e) => setDepositDescription(e.target.value)}
                    />
                  </div>
                  <p className="text-xs text-muted-foreground">
                    O depósito criará um Payment Intent do Stripe. O pagamento deve ser confirmado para creditar o saldo.
                  </p>
                  <div className="flex gap-3">
                    <Button variant="ghost" className="flex-1" onClick={closeDepositModal}>
                      Cancelar
                    </Button>
                    <Button
                      className="flex-1"
                      onClick={handleCreateDepositIntent}
                      disabled={depositLoading}
                    >
                      {depositLoading ? "Criando..." : "Criar Depósito"}
                    </Button>
                  </div>
                </>
              ) : (
                <>
                  <p className="text-sm text-muted-foreground">
                    Confirme o pagamento de{" "}
                    {formatCurrency(depositAmount ?? 0, account?.currency ?? "BRL")} no Stripe para liberar os fundos.
                  </p>
                  {stripePromise && stripeElementsOptions ? (
                    <Elements stripe={stripePromise} options={stripeElementsOptions}>
                      <DepositPaymentElement
                        amountLabel={formatCurrency(depositAmount ?? 0, account?.currency ?? "BRL")}
                        onBack={() => {
                          setDepositIntent(null);
                          setPaymentError(null);
                          setConfirmingPayment(false);
                        }}
                        onSuccess={handleDepositConfirmed}
                        processing={confirmingPayment}
                        setProcessing={setConfirmingPayment}
                        error={paymentError}
                        setError={setPaymentError}
                      />
                    </Elements>
                  ) : (
                    <p className="text-sm text-red-600">
                      Configure a chave pública do Stripe (NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY) para confirmar pagamentos.
                    </p>
                  )}
                </>
              )}
            </div>
          </div>
        </div>
      )}

      {/* Stripe Connect Modal (manual code entry) */}
      {showStripeModal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-card rounded-3xl border border-border/60 p-6 max-w-md w-full">
            <div className="flex justify-between items-start mb-4">
              <h3 className="text-lg font-semibold">Conectar Conta Stripe</h3>
              <button
                className="text-muted-foreground hover:text-foreground"
                onClick={() => setShowStripeModal(false)}
              >
                ✕
              </button>
            </div>
            <div className="space-y-4">
              <p className="text-sm text-muted-foreground">
                Informe o ID da conta Stripe Connect ou o código de autorização OAuth.
              </p>
              <input
                className="w-full rounded-xl border border-border bg-background/60 px-3 py-2 text-sm font-mono"
                placeholder="acct_xxx ou código OAuth"
                value={stripeCode}
                onChange={(e) => setStripeCode(e.target.value)}
              />
              <p className="text-xs text-muted-foreground">
                Para testes, use o Account ID (ex: <code>acct_1SlUdFC2a2OMnanf</code>).
              </p>
              <div className="flex gap-3">
                <Button
                  variant="ghost"
                  className="flex-1"
                  onClick={() => setShowStripeModal(false)}
                >
                  Cancelar
                </Button>
                <Button className="flex-1" onClick={handleConnectStripe}>
                  Conectar
                </Button>
              </div>
            </div>
          </div>
        </div>
      )}
    </LayoutWrapper>
  );
}

type DepositPaymentElementProps = {
  amountLabel: string;
  onBack: () => void;
  onSuccess: () => Promise<void>;
  processing: boolean;
  setProcessing: (value: boolean) => void;
  error: string | null;
  setError: (value: string | null) => void;
};

function DepositPaymentElement({
  amountLabel,
  onBack,
  onSuccess,
  processing,
  setProcessing,
  error,
  setError,
}: DepositPaymentElementProps) {
  const stripe = useStripe();
  const elements = useElements();

  const handleConfirm = async () => {
    if (!stripe || !elements) {
      setError("Stripe ainda não carregou.");
      return;
    }
    setError(null);
    setProcessing(true);
    try {
      const returnUrl =
        typeof window !== "undefined" ? window.location.href : undefined;
      const { error } = await stripe.confirmPayment({
        elements,
        confirmParams: returnUrl ? { return_url: returnUrl } : undefined,
        redirect: "if_required",
      });

      if (error) {
        setError(error.message ?? "Erro ao confirmar o pagamento.");
        return;
      }

      await onSuccess();
    } finally {
      setProcessing(false);
    }
  };

  return (
    <div className="space-y-4">
      <div className="rounded-2xl border border-border/60 bg-background/60 p-4">
        <p className="text-sm text-muted-foreground">
          Confirmar o valor {amountLabel} com o Stripe.
        </p>
        <div className="mt-3">
          <PaymentElement />
        </div>
      </div>
      <div className="flex gap-3">
        <Button variant="ghost" className="flex-1" onClick={onBack} disabled={processing}>
          Voltar
        </Button>
        <Button
          className="flex-1"
          onClick={handleConfirm}
          disabled={processing || !stripe}
        >
          {processing ? "Confirmando..." : "Confirmar pagamento"}
        </Button>
      </div>
      {error && <p className="text-sm text-red-600">{error}</p>}
    </div>
  );
}
