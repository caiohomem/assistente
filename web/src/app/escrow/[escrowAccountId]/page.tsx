"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { LayoutWrapper } from "@/components/LayoutWrapper";
import { Button } from "@/components/ui/button";
import {
  getEscrowAccountClient,
  listEscrowTransactionsClient,
} from "@/lib/api/escrowApiClient";
import type { EscrowAccountDto, EscrowTransactionDto } from "@/lib/types/escrow";

export default function EscrowLedgerPage({
  params,
}: {
  params: { escrowAccountId: string };
}) {
  const router = useRouter();
  const { escrowAccountId } = params;
  const [account, setAccount] = useState<EscrowAccountDto | null>(null);
  const [transactions, setTransactions] = useState<EscrowTransactionDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadAccount();
  }, [escrowAccountId]);

  async function loadAccount() {
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
  }

  const formatCurrency = (value: number, currency: string) => {
    try {
      return new Intl.NumberFormat("pt-BR", { style: "currency", currency }).format(value);
    } catch {
      return `${currency} ${value.toFixed(2)}`;
    }
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
        </div>
      )}
      {loading && <div className="text-sm text-muted-foreground">Carregando...</div>}
      {!loading && account && (
        <div className="space-y-6">
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
              <p className="text-sm font-semibold">
                {account.stripeConnectedAccountId ? account.stripeConnectedAccountId : "Não conectado"}
              </p>
            </div>
            <div className="flex items-end justify-end">
              <Button variant="outline" onClick={() => router.push(`/acordos/${account.agreementId}`)}>
                Voltar para o acordo
              </Button>
            </div>
          </div>

          <div className="rounded-3xl border border-border/60 bg-card/70 backdrop-blur-xl p-5">
            <div className="flex items-center justify-between mb-4">
              <div>
                <p className="text-xs uppercase tracking-widest text-primary/70">Ledger</p>
                <h3 className="text-lg font-semibold">Transações recentes</h3>
              </div>
              <Button variant="ghost" onClick={loadAccount}>
                Atualizar
              </Button>
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
                  </tr>
                </thead>
                <tbody>
                  {transactions.length === 0 && (
                    <tr>
                      <td className="py-4 text-muted-foreground" colSpan={5}>
                        Nenhuma transação registrada ainda.
                      </td>
                    </tr>
                  )}
                  {transactions.map((tx) => (
                    <tr key={tx.transactionId} className="border-b border-border/40">
                      <td className="py-2">
                        {new Date(tx.createdAt).toLocaleString("pt-BR", {
                          dateStyle: "short",
                          timeStyle: "short",
                        })}
                      </td>
                      <td className="py-2">{tx.type}</td>
                      <td className="py-2">{tx.status}</td>
                      <td className="py-2 font-semibold">
                        {formatCurrency(tx.amount, tx.currency)}
                      </td>
                      <td className="py-2 text-muted-foreground">{tx.description ?? "-"}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}
    </LayoutWrapper>
  );
}
