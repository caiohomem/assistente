"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { TopBar } from "@/components/TopBar";
import { ConfirmDialog } from "@/components/ConfirmDialog";
import { listDraftsClient, deleteDraftClient, type DraftDocument, type ListDraftsResult } from "@/lib/api/automationApiClient";
import { DraftStatus, DocumentType } from "@/lib/types/automation";

export default function DraftsPage() {
  const router = useRouter();
  const [loading, setLoading] = useState(true);
  const [drafts, setDrafts] = useState<DraftDocument[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [totalPages, setTotalPages] = useState(0);
  const [error, setError] = useState<string | null>(null);
  const [deleteDialog, setDeleteDialog] = useState<{ isOpen: boolean; draftId: string | null; draftType: string }>({
    isOpen: false,
    draftId: null,
    draftType: "",
  });
  const [deleting, setDeleting] = useState(false);

  useEffect(() => {
    loadDrafts();
  }, [page]);

  const loadDrafts = async () => {
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
  };

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

  if (loading && drafts.length === 0) {
    return (
      <div className="min-h-screen bg-zinc-50 dark:bg-zinc-900">
        <TopBar title="Rascunhos" showBackButton backHref="/dashboard" />
        <div className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
          <div className="text-center">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600 dark:border-indigo-400 mx-auto mb-4"></div>
            <p className="text-gray-600 dark:text-gray-300">Carregando rascunhos...</p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-900">
      <TopBar title="Rascunhos" showBackButton backHref="/dashboard">
        <Link
          href="/automacao/rascunhos/novo"
          className="inline-flex items-center justify-center rounded-md bg-indigo-600 dark:bg-indigo-500 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700 dark:hover:bg-indigo-600 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2"
        >
          Novo Rascunho
        </Link>
      </TopBar>
      <div className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
        {error && (
          <div className="mb-4 rounded-md bg-red-50 dark:bg-red-900/20 p-4">
            <p className="text-sm text-red-800 dark:text-red-200">{error}</p>
          </div>
        )}

        <div className="mb-6">
          <p className="text-sm text-zinc-600 dark:text-zinc-400">
            {total} {total === 1 ? "rascunho encontrado" : "rascunhos encontrados"}
          </p>
        </div>

        {drafts.length === 0 ? (
          <div className="rounded-lg bg-white dark:bg-zinc-800 p-8 text-center shadow">
            <p className="text-zinc-600 dark:text-zinc-400">Nenhum rascunho encontrado.</p>
            <Link
              href="/automacao/rascunhos/novo"
              className="mt-4 inline-block text-indigo-600 dark:text-indigo-400 hover:text-indigo-700 dark:hover:text-indigo-300"
            >
              Criar primeiro rascunho
            </Link>
          </div>
        ) : (
          <>
            <div className="space-y-4">
              {drafts.map((draft) => (
                <div
                  key={draft.draftId}
                  className="rounded-lg bg-white dark:bg-zinc-800 p-6 shadow hover:shadow-md transition-shadow"
                >
                  <Link
                    href={`/automacao/rascunhos/${draft.draftId}`}
                    className="block"
                  >
                    <div className="flex items-start justify-between">
                      <div className="flex-1">
                        <div className="flex items-center gap-3 mb-2">
                          <h3 className="text-lg font-semibold text-zinc-900 dark:text-zinc-100">
                            {getDocumentTypeLabel(draft.documentType)}
                          </h3>
                          <span
                            className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${getStatusColor(draft.status)}`}
                          >
                            {getStatusLabel(draft.status)}
                          </span>
                        </div>
                        <p className="text-sm text-zinc-600 dark:text-zinc-400 mb-2 line-clamp-2">
                          {draft.content}
                        </p>
                        <div className="flex items-center gap-4 text-sm text-zinc-500 dark:text-zinc-400">
                          <span>Criado em: {formatDate(draft.createdAt)}</span>
                          <span>Atualizado em: {formatDate(draft.updatedAt)}</span>
                        </div>
                      </div>
                    </div>
                  </Link>
                  <div className="mt-4 flex gap-2">
                    <Link
                      href={`/automacao/rascunhos/${draft.draftId}/editar`}
                      className="flex-1 text-center rounded-md border border-zinc-300 dark:border-zinc-700 bg-white dark:bg-zinc-800 px-3 py-2 text-sm font-medium text-zinc-700 dark:text-zinc-300 hover:bg-zinc-50 dark:hover:bg-zinc-700"
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
                <button
                  onClick={() => setPage(Math.max(1, page - 1))}
                  disabled={page === 1}
                  className="rounded-md border border-zinc-300 dark:border-zinc-700 bg-white dark:bg-zinc-800 px-4 py-2 text-sm font-medium text-zinc-700 dark:text-zinc-300 hover:bg-zinc-50 dark:hover:bg-zinc-700 disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  Anterior
                </button>
                <span className="text-sm text-zinc-600 dark:text-zinc-400">
                  Página {page} de {totalPages}
                </span>
                <button
                  onClick={() => setPage(Math.min(totalPages, page + 1))}
                  disabled={page === totalPages}
                  className="rounded-md border border-zinc-300 dark:border-zinc-700 bg-white dark:bg-zinc-800 px-4 py-2 text-sm font-medium text-zinc-700 dark:text-zinc-300 hover:bg-zinc-50 dark:hover:bg-zinc-700 disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  Próxima
                </button>
              </div>
            )}
          </>
        )}
      </div>
    </div>
  );
}

