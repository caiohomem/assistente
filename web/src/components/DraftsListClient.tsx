"use client";

import { useEffect, useState, useCallback } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import Link from "next/link";
import { ConfirmDialog } from "@/components/ConfirmDialog";
import { listDraftsClient, deleteDraftClient, type DraftDocument, type ListDraftsResult } from "@/lib/api/automationApiClient";
import { DraftStatus, DocumentType } from "@/lib/types/automation";
import { NovoRascunhoClient } from "@/app/automacao/rascunhos/novo/NovoRascunhoClient";
import { ArrowLeft } from "lucide-react";
import { Button } from "@/components/ui/button";

interface DraftsListClientProps {
  initialData?: ListDraftsResult;
}

export function DraftsListClient({ initialData }: DraftsListClientProps) {
  const router = useRouter();
  const searchParams = useSearchParams();
  const showNewDraft = searchParams.get("novo") === "true";
  
  const [loading, setLoading] = useState(!initialData);
  const [drafts, setDrafts] = useState<DraftDocument[]>(initialData?.drafts || []);
  const [total, setTotal] = useState(initialData?.total || 0);
  const [page, setPage] = useState(initialData?.page || 1);
  const [pageSize] = useState(initialData?.pageSize || 20);
  const [totalPages, setTotalPages] = useState(initialData?.totalPages || 0);
  const [error, setError] = useState<string | null>(null);
  const [deleteDialog, setDeleteDialog] = useState<{ isOpen: boolean; draftId: string | null; draftType: string }>({
    isOpen: false,
    draftId: null,
    draftType: "",
  });
  const [deleting, setDeleting] = useState(false);

  const loadDrafts = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await listDraftsClient({
        page,
        pageSize,
      });
      setDrafts(result.drafts);
      setTotal(result.total);
      setPage(result.page);
      setTotalPages(result.totalPages);
    } catch (err) {
      console.error("Erro ao carregar rascunhos:", err);
      setError(err instanceof Error ? err.message : "Erro ao carregar rascunhos");
    } finally {
      setLoading(false);
    }
  }, [page, pageSize]);

  useEffect(() => {
    if (!initialData) {
      loadDrafts();
    }
  }, [initialData, loadDrafts]);

  const getStatusLabel = (status: DraftStatus): string => {
    switch (status) {
      case DraftStatus.Draft:
        return "Rascunho";
      case DraftStatus.Approved:
        return "Aprovado";
      case DraftStatus.Sent:
        return "Enviado";
      default:
        return "Desconhecido";
    }
  };

  const getStatusColor = (status: DraftStatus): string => {
    switch (status) {
      case DraftStatus.Draft:
        return "bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-200";
      case DraftStatus.Approved:
        return "bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200";
      case DraftStatus.Sent:
        return "bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200";
      default:
        return "bg-gray-100 text-gray-800";
    }
  };

  const getDocumentTypeLabel = (type: DocumentType): string => {
    switch (type) {
      case DocumentType.Email:
        return "E-mail";
      case DocumentType.Oficio:
        return "Ofício";
      case DocumentType.Invite:
        return "Convite";
      default:
        return "Desconhecido";
    }
  };

  const formatDate = (dateString: string): string => {
    return new Date(dateString).toLocaleDateString("pt-BR", {
      day: "2-digit",
      month: "2-digit",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
  };

  const handleDeleteClick = (draftId: string, draftType: string) => {
    setDeleteDialog({
      isOpen: true,
      draftId,
      draftType,
    });
  };

  const handleDeleteConfirm = async () => {
    if (!deleteDialog.draftId) return;

    setDeleting(true);
    try {
      await deleteDraftClient(deleteDialog.draftId);
      setDeleteDialog({ isOpen: false, draftId: null, draftType: "" });
      await loadDrafts();
      router.refresh();
    } catch (err) {
      console.error("Erro ao deletar rascunho:", err);
      setError(err instanceof Error ? err.message : "Erro ao deletar rascunho");
    } finally {
      setDeleting(false);
    }
  };

  const handleDeleteCancel = () => {
    setDeleteDialog({ isOpen: false, draftId: null, draftType: "" });
  };

  const handleNewDraftCancel = () => {
    router.push("/documentos");
  };

  const handleNewDraftSuccess = async () => {
    router.push("/documentos");
    router.refresh();
    await loadDrafts();
  };

  return (
    <div className="space-y-6">
      {/* Formulário de Novo Rascunho - aparece quando ?novo=true */}
      {showNewDraft && (
        <div className="glass-card p-6 animate-slide-up">
          <div className="flex items-center justify-between mb-6">
            <h2 className="text-2xl font-semibold">Novo Rascunho</h2>
            <Button
              variant="ghost"
              size="icon"
              onClick={handleNewDraftCancel}
              className="rounded-lg"
            >
              <ArrowLeft className="w-4 h-4" />
            </Button>
          </div>
          <NovoRascunhoClient onSuccess={handleNewDraftSuccess} onCancel={handleNewDraftCancel} />
        </div>
      )}

      {/* Lista de Rascunhos - sempre visível */}
      <div>
        {error && (
          <div className="mb-4 rounded-md bg-red-50 dark:bg-red-900/20 p-4">
            <p className="text-sm text-red-800 dark:text-red-200">{error}</p>
          </div>
        )}

        <div className="mb-6">
          <p className="text-sm text-muted-foreground">
            {total} {total === 1 ? "rascunho encontrado" : "rascunhos encontrados"}
          </p>
        </div>

        {loading && drafts.length === 0 ? (
          <div className="glass-card p-12 text-center">
            <p className="text-muted-foreground">Carregando rascunhos...</p>
          </div>
        ) : drafts.length === 0 ? (
          <div className="glass-card p-12 text-center">
            <p className="text-muted-foreground">Nenhum rascunho encontrado.</p>
            <button
              onClick={() => router.push("/documentos?tab=drafts&novo=true")}
              className="mt-4 inline-block text-primary hover:underline"
            >
              Criar primeiro rascunho
            </button>
          </div>
        ) : (
          <>
            <div className="space-y-4">
              {drafts.map((draft) => (
                <div
                  key={draft.draftId}
                  className="glass-card p-6 hover:shadow-md transition-shadow"
                >
                  <Link
                    href={`/automacao/rascunhos/${draft.draftId}`}
                    className="block"
                  >
                    <div className="flex items-start justify-between">
                      <div className="flex-1">
                        <div className="flex items-center gap-3 mb-2">
                          <h3 className="text-lg font-semibold">
                            {getDocumentTypeLabel(draft.documentType)}
                          </h3>
                          <span
                            className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${getStatusColor(draft.status)}`}
                          >
                            {getStatusLabel(draft.status)}
                          </span>
                        </div>
                        <p className="text-sm text-muted-foreground mb-2 line-clamp-2">
                          {draft.content}
                        </p>
                        <div className="flex items-center gap-4 text-sm text-muted-foreground">
                          <span>Criado em: {formatDate(draft.createdAt)}</span>
                          <span>Atualizado em: {formatDate(draft.updatedAt)}</span>
                        </div>
                      </div>
                    </div>
                  </Link>
                  <div className="mt-4 flex gap-2">
                    <Link
                      href={`/automacao/rascunhos/${draft.draftId}/editar`}
                      className="flex-1 text-center rounded-md border border-border bg-secondary/50 px-3 py-2 text-sm font-medium hover:bg-secondary/80 transition-colors"
                    >
                      Editar
                    </Link>
                    <button
                      onClick={(e) => {
                        e.preventDefault();
                        handleDeleteClick(draft.draftId, getDocumentTypeLabel(draft.documentType));
                      }}
                      className="flex-1 rounded-md border border-red-300 dark:border-red-700 bg-white dark:bg-zinc-800 px-3 py-2 text-sm font-medium text-red-700 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20"
                    >
                      Excluir
                    </button>
                  </div>
                </div>
              ))}
            </div>
            <ConfirmDialog
              isOpen={deleteDialog.isOpen}
              title="Excluir Rascunho"
              message={`Tem certeza que deseja excluir este rascunho (${deleteDialog.draftType})? Esta ação não pode ser desfeita.`}
              confirmText="Excluir"
              cancelText="Cancelar"
              onConfirm={handleDeleteConfirm}
              onCancel={handleDeleteCancel}
              isLoading={deleting}
              variant="danger"
            />

            {totalPages > 1 && (
              <div className="mt-6 flex items-center justify-between">
                <Button
                  variant="ghost"
                  onClick={() => setPage(Math.max(1, page - 1))}
                  disabled={page === 1}
                >
                  Anterior
                </Button>
                <span className="text-sm text-muted-foreground">
                  Página {page} de {totalPages}
                </span>
                <Button
                  variant="ghost"
                  onClick={() => setPage(Math.min(totalPages, page + 1))}
                  disabled={page === totalPages}
                >
                  Próxima
                </Button>
              </div>
            )}
          </>
        )}
      </div>
    </div>
  );
}
