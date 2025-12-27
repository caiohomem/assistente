"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { TopBar } from "@/components/TopBar";
import { ConfirmDialog } from "@/components/ConfirmDialog";
import { listRemindersClient, deleteReminderClient, type Reminder, type ListRemindersResult } from "@/lib/api/automationApiClient";
import { ReminderStatus } from "@/lib/types/automation";

export default function RemindersPage() {
  const router = useRouter();
  const [loading, setLoading] = useState(true);
  const [reminders, setReminders] = useState<Reminder[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [totalPages, setTotalPages] = useState(0);
  const [error, setError] = useState<string | null>(null);
  const [deleteDialog, setDeleteDialog] = useState<{ isOpen: boolean; reminderId: string | null; reminderReason: string }>({
    isOpen: false,
    reminderId: null,
    reminderReason: "",
  });
  const [deleting, setDeleting] = useState(false);

  useEffect(() => {
    loadReminders();
  }, [page]);

  const loadReminders = async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await listRemindersClient({
        page,
        pageSize,
      });
      setReminders(result.reminders);
      setTotal(result.total);
      setPage(result.page);
      setTotalPages(result.totalPages);
    } catch (err) {
      console.error("Erro ao carregar lembretes:", err);
      setError(err instanceof Error ? err.message : "Erro ao carregar lembretes");
    } finally {
      setLoading(false);
    }
  };

  const getStatusLabel = (status: ReminderStatus): string => {
    switch (status) {
      case ReminderStatus.Pending:
        return "Pendente";
      case ReminderStatus.Sent:
        return "Enviado";
      case ReminderStatus.Dismissed:
        return "Dispensado";
      case ReminderStatus.Snoozed:
        return "Adiado";
      default:
        return "Desconhecido";
    }
  };

  const getStatusColor = (status: ReminderStatus): string => {
    switch (status) {
      case ReminderStatus.Pending:
        return "bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-200";
      case ReminderStatus.Sent:
        return "bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200";
      case ReminderStatus.Dismissed:
        return "bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-200";
      case ReminderStatus.Snoozed:
        return "bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200";
      default:
        return "bg-gray-100 text-gray-800";
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

  const handleDeleteClick = (reminderId: string, reminderReason: string) => {
    setDeleteDialog({
      isOpen: true,
      reminderId,
      reminderReason,
    });
  };

  const handleDeleteConfirm = async () => {
    if (!deleteDialog.reminderId) return;

    setDeleting(true);
    try {
      await deleteReminderClient(deleteDialog.reminderId);
      setDeleteDialog({ isOpen: false, reminderId: null, reminderReason: "" });
      await loadReminders();
      router.refresh();
    } catch (err) {
      console.error("Erro ao deletar lembrete:", err);
      setError(err instanceof Error ? err.message : "Erro ao deletar lembrete");
    } finally {
      setDeleting(false);
    }
  };

  const handleDeleteCancel = () => {
    setDeleteDialog({ isOpen: false, reminderId: null, reminderReason: "" });
  };

  if (loading && reminders.length === 0) {
    return (
      <div className="min-h-screen bg-zinc-50 dark:bg-zinc-900">
        <TopBar title="Lembretes" showBackButton backHref="/dashboard" />
        <div className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
          <div className="text-center">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600 dark:border-indigo-400 mx-auto mb-4"></div>
            <p className="text-gray-600 dark:text-gray-300">Carregando lembretes...</p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-900">
      <TopBar title="Lembretes" showBackButton backHref="/dashboard">
        <Link
          href="/automacao/lembretes/novo"
          className="inline-flex items-center justify-center rounded-md bg-indigo-600 dark:bg-indigo-500 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700 dark:hover:bg-indigo-600 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2"
        >
          Novo Lembrete
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
            {total} {total === 1 ? "lembrete encontrado" : "lembretes encontrados"}
          </p>
        </div>

        {reminders.length === 0 ? (
          <div className="rounded-lg bg-white dark:bg-zinc-800 p-8 text-center shadow">
            <p className="text-zinc-600 dark:text-zinc-400">Nenhum lembrete encontrado.</p>
            <Link
              href="/automacao/lembretes/novo"
              className="mt-4 inline-block text-indigo-600 dark:text-indigo-400 hover:text-indigo-700 dark:hover:text-indigo-300"
            >
              Criar primeiro lembrete
            </Link>
          </div>
        ) : (
          <>
            <div className="space-y-4">
              {reminders.map((reminder) => (
                <div
                  key={reminder.reminderId}
                  className="rounded-lg bg-white dark:bg-zinc-800 p-6 shadow hover:shadow-md transition-shadow"
                >
                  <div className="flex items-start justify-between">
                    <div className="flex-1">
                      <div className="flex items-center gap-3 mb-2">
                        <h3 className="text-lg font-semibold text-zinc-900 dark:text-zinc-100">
                          {reminder.reason}
                        </h3>
                        <span
                          className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${getStatusColor(reminder.status)}`}
                        >
                          {getStatusLabel(reminder.status)}
                        </span>
                      </div>
                      {reminder.suggestedMessage && (
                        <p className="text-sm text-zinc-600 dark:text-zinc-400 mb-2">
                          {reminder.suggestedMessage}
                        </p>
                      )}
                      <div className="flex items-center gap-4 text-sm text-zinc-500 dark:text-zinc-400">
                        <span>Agendado para: {formatDate(reminder.scheduledFor)}</span>
                        <span>Criado em: {formatDate(reminder.createdAt)}</span>
                      </div>
                    </div>
                  </div>
                  <div className="mt-4 flex gap-2">
                    <Link
                      href={`/automacao/lembretes/${reminder.reminderId}/editar`}
                      className="flex-1 text-center rounded-md border border-zinc-300 dark:border-zinc-700 bg-white dark:bg-zinc-800 px-3 py-2 text-sm font-medium text-zinc-700 dark:text-zinc-300 hover:bg-zinc-50 dark:hover:bg-zinc-700"
                    >
                      Editar
                    </Link>
                    <button
                      onClick={() => handleDeleteClick(reminder.reminderId, reminder.reason)}
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
              title="Excluir Lembrete"
              message={`Tem certeza que deseja excluir o lembrete "${deleteDialog.reminderReason}"? Esta ação não pode ser desfeita.`}
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

