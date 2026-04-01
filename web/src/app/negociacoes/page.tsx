"use client";

import { useEffect, useState } from "react";
import { LayoutWrapper } from "@/components/LayoutWrapper";
import { Button } from "@/components/ui/button";
import {
  acceptNegotiationProposalClient,
  createNegotiationSessionClient,
  getNegotiationSessionClient,
  listNegotiationSessionsClient,
  rejectNegotiationProposalClient,
  requestAiProposalClient,
  submitNegotiationProposalClient,
} from "@/lib/api/negotiationsApiClient";
import type { NegotiationProposalDto, NegotiationSessionDto } from "@/lib/types/negotiation";

export default function NegotiationsPage() {
  const [sessions, setSessions] = useState<NegotiationSessionDto[]>([]);
  const [selectedSession, setSelectedSession] = useState<NegotiationSessionDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [newSessionTitle, setNewSessionTitle] = useState("");
  const [newSessionContext, setNewSessionContext] = useState("");
  const [proposalContent, setProposalContent] = useState("");
  const [aiInstructions, setAiInstructions] = useState("");
  const [rejectReason, setRejectReason] = useState("");

  useEffect(() => {
    loadSessions();
  }, []);

  async function loadSessions() {
    setLoading(true);
    setError(null);
    try {
      const list = await listNegotiationSessionsClient();
      setSessions(list);
      if (list.length > 0) {
        setSelectedSession(list[0]);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Erro ao carregar negociações.");
    } finally {
      setLoading(false);
    }
  }

  async function refreshSession(sessionId: string) {
    try {
      const session = await getNegotiationSessionClient(sessionId);
      setSelectedSession(session);
      setSessions((prev) =>
        prev.map((item) => (item.sessionId === sessionId ? session : item)),
      );
    } catch (err) {
      setError(err instanceof Error ? err.message : "Erro ao atualizar sessão.");
    }
  }

  async function handleCreateSession() {
    if (!newSessionTitle) {
      setError("Informe um título para a negociação.");
      return;
    }
    try {
      await createNegotiationSessionClient({
        title: newSessionTitle,
        context: newSessionContext,
      });
      setNewSessionContext("");
      setNewSessionTitle("");
      await loadSessions();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Erro ao criar negociação.");
    }
  }

  async function handleSubmitProposal() {
    if (!selectedSession) return;
    if (!proposalContent) {
      setError("Descreva a proposta antes de enviar.");
      return;
    }
    try {
      await submitNegotiationProposalClient({
        sessionId: selectedSession.sessionId,
        content: proposalContent,
      });
      setProposalContent("");
      await refreshSession(selectedSession.sessionId);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Erro ao enviar proposta.");
    }
  }

  async function handleRequestAi() {
    if (!selectedSession) return;
    try {
      await requestAiProposalClient({
        sessionId: selectedSession.sessionId,
        instructions: aiInstructions,
      });
      setAiInstructions("");
      await refreshSession(selectedSession.sessionId);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Erro ao solicitar sugestão da IA.");
    }
  }

  async function handleAcceptProposal(proposal: NegotiationProposalDto) {
    if (!selectedSession) return;
    try {
      await acceptNegotiationProposalClient({
        sessionId: selectedSession.sessionId,
        proposalId: proposal.proposalId,
      });
      await refreshSession(selectedSession.sessionId);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Erro ao aceitar proposta.");
    }
  }

  async function handleRejectProposal(proposal: NegotiationProposalDto) {
    if (!selectedSession) return;
    if (!rejectReason) {
      setError("Informe o motivo da rejeição.");
      return;
    }
    try {
      await rejectNegotiationProposalClient({
        sessionId: selectedSession.sessionId,
        proposalId: proposal.proposalId,
        reason: rejectReason,
      });
      setRejectReason("");
      await refreshSession(selectedSession.sessionId);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Erro ao rejeitar proposta.");
    }
  }

  return (
    <LayoutWrapper
      title="Negociações assistidas por IA"
      subtitle="Centralize propostas, receba sugestões inteligentes e avance acordos mais rápido."
      activeTab="negotiations"
    >
      {error && (
        <div className="rounded-xl border border-red-200 bg-red-50/80 text-red-700 px-4 py-3 text-sm mb-4">
          {error}
        </div>
      )}
      {loading && <div className="text-sm text-muted-foreground">Carregando...</div>}
      {!loading && (
        <div className="grid grid-cols-1 xl:grid-cols-3 gap-6">
          <section className="rounded-3xl border border-border/60 bg-card/70 backdrop-blur-xl p-5 space-y-5">
            <div>
              <p className="text-xs uppercase tracking-widest text-primary/70">Pipeline</p>
              <h2 className="text-xl font-semibold">Sessões em andamento</h2>
            </div>
            <div className="space-y-3 max-h-[600px] overflow-auto pr-2">
              {sessions.length === 0 && (
                <p className="text-sm text-muted-foreground">
                  Nenhuma negociação aberta. Crie uma nova ao lado.
                </p>
              )}
              {sessions.map((session) => (
                <button
                  key={session.sessionId}
                  className={`w-full text-left rounded-2xl border border-border/60 p-4 ${
                    selectedSession?.sessionId === session.sessionId
                      ? "bg-primary/10 border-primary/40"
                      : "bg-background/50"
                  }`}
                  onClick={() => setSelectedSession(session)}
                >
                  <p className="font-semibold">{session.title}</p>
                  <p className="text-xs text-muted-foreground mb-2">
                    {session.status} • Atualizado em{" "}
                    {new Date(session.updatedAt).toLocaleDateString()}
                  </p>
                  <div className="flex items-center gap-3 text-xs">
                    <span className="px-2 py-1 rounded-full bg-border/40">
                      {session.pendingProposalCount ?? session.proposals.filter((p) => p.status === "Pending" || p.status === 1).length} pendentes
                    </span>
                    <span className="px-2 py-1 rounded-full bg-border/40">
                      {session.aiProposalCount ?? session.proposals.filter((p) => p.source === "AI" || p.source === 2).length} IA
                    </span>
                  </div>
                </button>
              ))}
            </div>
            <div className="border-t border-border/40 pt-4 space-y-3 text-sm">
              <p className="text-xs uppercase tracking-widest text-muted-foreground">
                Nova sessão
              </p>
              <input
                className="w-full rounded-xl border border-border bg-background/60 px-3 py-2"
                placeholder="Título"
                value={newSessionTitle}
                onChange={(e) => setNewSessionTitle(e.target.value)}
              />
              <textarea
                className="w-full rounded-xl border border-border bg-background/60 px-3 py-2 min-h-[80px]"
                placeholder="Contexto, objetivos, restrições..."
                value={newSessionContext}
                onChange={(e) => setNewSessionContext(e.target.value)}
              />
              <Button className="w-full" onClick={handleCreateSession}>
                Criar sessão
              </Button>
            </div>
          </section>

          <section className="xl:col-span-2 rounded-3xl border border-border/60 bg-card/70 backdrop-blur-xl p-5 space-y-5">
            {selectedSession ? (
              <>
                <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-3">
                  <div>
                    <p className="text-xs uppercase tracking-widest text-primary/70">
                      Sessão ativa
                    </p>
                    <h2 className="text-2xl font-semibold">{selectedSession.title}</h2>
                    <p className="text-xs text-muted-foreground">
                      Status: {selectedSession.status} • Criado em{" "}
                      {new Date(selectedSession.createdAt).toLocaleDateString()}
                    </p>
                  </div>
                  <div className="text-xs text-muted-foreground">
                    {selectedSession.aiSuggestionCooldownActive
                      ? `Próxima sugestão de IA em ${selectedSession.nextAiSuggestionAvailableAt ? new Date(selectedSession.nextAiSuggestionAvailableAt).toLocaleTimeString() : ""}`
                      : "IA disponível"}
                  </div>
                </div>

                <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
                  <div className="rounded-2xl border border-border/60 bg-background/50 p-4 space-y-3">
                    <p className="text-xs uppercase tracking-widest text-muted-foreground">
                      Proposta manual
                    </p>
                    <textarea
                      className="w-full rounded-xl border border-border bg-background/60 px-3 py-2 min-h-[120px]"
                      placeholder="Descreva valores, condições, contrapartidas..."
                      value={proposalContent}
                      onChange={(e) => setProposalContent(e.target.value)}
                    />
                    <Button className="w-full" onClick={handleSubmitProposal}>
                      Enviar proposta
                    </Button>
                  </div>
                  <div className="rounded-2xl border border-border/60 bg-background/50 p-4 space-y-3">
                    <p className="text-xs uppercase tracking-widest text-muted-foreground">
                      Sugestão da IA
                    </p>
                    <textarea
                      className="w-full rounded-xl border border-border bg-background/60 px-3 py-2 min-h-[120px]"
                      placeholder="Instruções adicionais para a IA (opcional)"
                      value={aiInstructions}
                      onChange={(e) => setAiInstructions(e.target.value)}
                    />
                    <Button
                      className="w-full"
                      onClick={handleRequestAi}
                      disabled={selectedSession.aiSuggestionCooldownActive}
                    >
                      Solicitar sugestão
                    </Button>
                    {selectedSession.aiSuggestionCooldownActive && (
                      <p className="text-xs text-muted-foreground text-center">
                        Aguardando cooldown para nova sugestão.
                      </p>
                    )}
                  </div>
                </div>

                <div className="rounded-2xl border border-border/60 bg-background/50 p-4">
                  <div className="flex items-center justify-between mb-4">
                    <h3 className="text-lg font-semibold">Histórico de propostas</h3>
                    <Button variant="ghost" onClick={() => refreshSession(selectedSession.sessionId)}>
                      Atualizar
                    </Button>
                  </div>
                  <div className="space-y-3 max-h-[420px] overflow-auto pr-2">
                    {selectedSession.proposals.length === 0 && (
                      <p className="text-sm text-muted-foreground">Nenhuma proposta registrada.</p>
                    )}
                     {selectedSession.proposals.map((proposal) => (
                      <div
                        key={proposal.proposalId}
                        className="rounded-2xl border border-border/60 bg-card/60 p-4 space-y-2"
                      >
                        <div className="flex items-center justify-between text-xs text-muted-foreground">
                          <span>
                            {proposal.source === "AI" || proposal.source === 2 ? "IA" : "Parte"}
                          </span>
                          <span>Status: {proposal.status}</span>
                          <span>
                            {new Date(proposal.createdAt).toLocaleString("pt-BR", {
                              dateStyle: "short",
                              timeStyle: "short",
                            })}
                          </span>
                        </div>
                        <div className="text-sm whitespace-pre-wrap">{proposal.content}</div>
                        {proposal.rejectionReason && (
                          <p className="text-xs text-muted-foreground">
                            Motivo rejeição: {proposal.rejectionReason}
                          </p>
                        )}
                        {proposal.status === "Pending" || proposal.status === 1 ? (
                          <div className="flex items-center gap-3">
                            <Button variant="ghost" size="sm" onClick={() => handleAcceptProposal(proposal)}>
                              Aceitar
                            </Button>
                            <div className="flex-1 flex gap-2">
                              <input
                                className="flex-1 rounded-xl border border-border bg-background/60 px-3 py-2 text-xs"
                                placeholder="Motivo rejeição"
                                value={rejectReason}
                                onChange={(e) => setRejectReason(e.target.value)}
                              />
                              <Button
                                variant="ghost"
                                size="sm"
                                onClick={() => handleRejectProposal(proposal)}
                              >
                                Rejeitar
                              </Button>
                            </div>
                          </div>
                        ) : null}
                      </div>
                    ))}
                  </div>
                </div>
              </>
            ) : (
              <div className="text-sm text-muted-foreground">
                Selecione uma sessão para visualizar os detalhes.
              </div>
            )}
          </section>
        </div>
      )}
    </LayoutWrapper>
  );
}
