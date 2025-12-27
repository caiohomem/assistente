"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { TopBar } from "@/components/TopBar";
import { ConfirmDialog } from "@/components/ConfirmDialog";
import { listLetterheadsClient, deleteLetterheadClient, type Letterhead, type ListLetterheadsResult } from "@/lib/api/automationApiClient";

export default function LetterheadsPage() {
  const router = useRouter();
  const [loading, setLoading] = useState(true);
  const [letterheads, setLetterheads] = useState<Letterhead[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [totalPages, setTotalPages] = useState(0);
  const [error, setError] = useState<string | null>(null);
  const [deleteDialog, setDeleteDialog] = useState<{ isOpen: boolean; letterheadId: string | null; letterheadName: string }>({
    isOpen: false,
    letterheadId: null,
    letterheadName: "",
  });
  const [deleting, setDeleting] = useState(false);

  useEffect(() => {
    loadLetterheads();
  }, [page]);

  const loadLetterheads = async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await listLetterheadsClient({
        page,
        pageSize,
      });
      setLetterheads(result.letterheads);
      setTotal(result.total);
      setPage(result.page);
      setTotalPages(result.totalPages);
    } catch (err) {
      console.error("Erro ao carregar papéis timbrados:", err);
      setError(err instanceof Error ? err.message : "Erro ao carregar papéis timbrados");
    } finally {
      setLoading(false);
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

  const handleDeleteClick = (letterheadId: string, letterheadName: string) => {
    setDeleteDialog({
      isOpen: true,
      letterheadId,
      letterheadName,
    });
  };

  const handleDeleteConfirm = async () => {
    if (!deleteDialog.letterheadId) return;

    setDeleting(true);
    try {
      await deleteLetterheadClient(deleteDialog.letterheadId);
      setDeleteDialog({ isOpen: false, letterheadId: null, letterheadName: "" });
      await loadLetterheads();
      router.refresh();
    } catch (err) {
      console.error("Erro ao deletar papel timbrado:", err);
      setError(err instanceof Error ? err.message : "Erro ao deletar papel timbrado");
    } finally {
      setDeleting(false);
    }
  };

  const handleDeleteCancel = () => {
    setDeleteDialog({ isOpen: false, letterheadId: null, letterheadName: "" });
  };

  if (loading && letterheads.length === 0) {
    return (
      <div className="min-h-screen bg-zinc-50 dark:bg-zinc-900">
        <TopBar title="Papéis Timbrados" showBackButton backHref="/dashboard" />
        <div className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
          <div className="text-center">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600 dark:border-indigo-400 mx-auto mb-4"></div>
            <p className="text-gray-600 dark:text-gray-300">Carregando papéis timbrados...</p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-900">
      <TopBar title="Papéis Timbrados" showBackButton backHref="/dashboard">
        <Link
          href="/automacao/papeis-timbrados/novo"
          className="inline-flex items-center justify-center rounded-md bg-indigo-600 dark:bg-indigo-500 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700 dark:hover:bg-indigo-600 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2"
        >
          Novo Papel Timbrado
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
            {total} {total === 1 ? "papel timbrado encontrado" : "papéis timbrados encontrados"}
          </p>
        </div>

        {letterheads.length === 0 ? (
          <div className="rounded-lg bg-white dark:bg-zinc-800 p-8 text-center shadow">
            <p className="text-zinc-600 dark:text-zinc-400">Nenhum papel timbrado encontrado.</p>
            <Link
              href="/automacao/papeis-timbrados/novo"
              className="mt-4 inline-block text-indigo-600 dark:text-indigo-400 hover:text-indigo-700 dark:hover:text-indigo-300"
            >
              Criar primeiro papel timbrado
            </Link>
          </div>
        ) : (
          <>
            <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
              {letterheads.map((letterhead) => (
                <div
                  key={letterhead.letterheadId}
                  className="rounded-lg bg-white dark:bg-zinc-800 p-6 shadow hover:shadow-md transition-shadow"
                >
                  <Link
                    href={`/automacao/papeis-timbrados/${letterhead.letterheadId}`}
                    className="block"
                  >
                    <div className="flex items-start justify-between mb-2">
                      <h3 className="text-lg font-semibold text-zinc-900 dark:text-zinc-100">
                        {letterhead.name}
                      </h3>
                      {letterhead.isActive ? (
                        <span className="inline-flex items-center rounded-full bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200 px-2.5 py-0.5 text-xs font-medium">
                          Ativo
                        </span>
                      ) : (
                        <span className="inline-flex items-center rounded-full bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-200 px-2.5 py-0.5 text-xs font-medium">
                          Inativo
                        </span>
                      )}
                    </div>
                    <p className="text-xs text-zinc-500 dark:text-zinc-400">
                      Criado em: {formatDate(letterhead.createdAt)}
                    </p>
                  </Link>
                  <div className="mt-4 flex gap-2">
                    <Link
                      href={`/automacao/papeis-timbrados/${letterhead.letterheadId}/editar`}
                      className="flex-1 text-center rounded-md border border-zinc-300 dark:border-zinc-700 bg-white dark:bg-zinc-800 px-3 py-2 text-sm font-medium text-zinc-700 dark:text-zinc-300 hover:bg-zinc-50 dark:hover:bg-zinc-700"
                    >
                      Editar
                    </Link>
                    <button
                      onClick={(e) => {
                        e.preventDefault();
                        handleDeleteClick(letterhead.letterheadId, letterhead.name);
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
              title="Excluir Papel Timbrado"
              message={`Tem certeza que deseja excluir o papel timbrado "${deleteDialog.letterheadName}"? Esta ação não pode ser desfeita.`}
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

