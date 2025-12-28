"use client";

import { useEffect, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import Link from "next/link";
import { ConfirmDialog } from "@/components/ConfirmDialog";
import { listRemindersClient, deleteReminderClient, type Reminder, type ListRemindersResult } from "@/lib/api/automationApiClient";
import { ReminderStatus } from "@/lib/types/automation";
import { NovoLembreteClient } from "@/app/automacao/lembretes/novo/NovoLembreteClient";
import { ArrowLeft, Edit, Trash2, Calendar, Clock } from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

interface RemindersListClientProps {
  initialData?: ListRemindersResult;
}

export function RemindersListClient({ initialData }: RemindersListClientProps) {
  const router = useRouter();
  const searchParams = useSearchParams();
  const showNewReminder = searchParams.get("novo") === "true";
  
  const [loading, setLoading] = useState(!initialData);
  const [reminders, setReminders] = useState<Reminder[]>(initialData?.reminders || []);
  const [total, setTotal] = useState(initialData?.total || 0);
  const [page, setPage] = useState(initialData?.page || 1);
  const [pageSize] = useState(initialData?.pageSize || 20);
  const [totalPages, setTotalPages] = useState(initialData?.totalPages || 0);
  const [error, setError] = useState<string | null>(null);
  const [deleteDialog, setDeleteDialog] = useState<{ isOpen: boolean; reminderId: string | null; reminderReason: string }>({
    isOpen: false,
    reminderId: null,
    reminderReason: "",
  });
  const [deleting, setDeleting] = useState(false);

  useEffect(() => {
    if (!initialData) {
      loadReminders();
    }
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

  const handleNewReminderCancel = () => {
    router.push("/automacao/lembretes");
  };

  const handleNewReminderSuccess = async () => {
    router.push("/automacao/lembretes");
    router.refresh();
    await loadReminders();
  };

  return (
    <div className="space-y-6">
      {/* Formulário de Novo Lembrete - aparece quando ?novo=true */}
      {showNewReminder && (
        <div className="glass-card p-6 animate-slide-up">
          <div className="flex items-center justify-between mb-6">
            <h2 className="text-2xl font-semibold">Novo Lembrete</h2>
            <Button
              variant="ghost"
              size="icon"
              onClick={handleNewReminderCancel}
              className="rounded-lg"
            >
              <ArrowLeft className="w-4 h-4" />
            </Button>
          </div>
          <NovoLembreteClient onSuccess={handleNewReminderSuccess} onCancel={handleNewReminderCancel} />
        </div>
      )}

      {/* Lista de Lembretes - sempre visível */}
      <div>
        {error && (
          <div className="mb-4 glass-card border-destructive/50 bg-destructive/10 p-4">
            <p className="text-sm text-destructive">{error}</p>
          </div>
        )}

        {loading && reminders.length === 0 ? (
          <div className="glass-card p-12 text-center">
            <p className="text-muted-foreground">Carregando lembretes...</p>
          </div>
        ) : reminders.length === 0 ? (
          <div className="glass-card p-12 text-center">
            <p className="text-muted-foreground mb-4">Nenhum lembrete encontrado.</p>
            <button
              onClick={() => router.push("/automacao/lembretes?novo=true")}
              className="inline-block text-primary hover:underline"
            >
              Criar primeiro lembrete
            </button>
          </div>
        ) : (
          <>
            <div className="space-y-4">
              {reminders.map((reminder) => (
                <div
                  key={reminder.reminderId}
                  className="glass-card p-6 card-hover"
                >
                  <div className="flex items-start justify-between mb-4">
                    <div className="flex-1">
                      <div className="flex items-center gap-3 mb-2 flex-wrap">
                        <h3 className="text-lg font-semibold">
                          {reminder.reason}
                        </h3>
                        <span
                          className={cn(
                            "inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium",
                            reminder.status === ReminderStatus.Pending && "bg-warning/10 text-warning",
                            reminder.status === ReminderStatus.Sent && "bg-success/10 text-success",
                            reminder.status === ReminderStatus.Dismissed && "bg-muted text-muted-foreground",
                            reminder.status === ReminderStatus.Snoozed && "bg-primary/10 text-primary"
                          )}
                        >
                          {getStatusLabel(reminder.status)}
                        </span>
                      </div>
                      {reminder.suggestedMessage && (
                        <p className="text-sm text-muted-foreground mb-3">
                          {reminder.suggestedMessage}
                        </p>
                      )}
                      <div className="flex items-center gap-4 text-sm text-muted-foreground flex-wrap">
                        <span className="flex items-center gap-1">
                          <Calendar className="w-4 h-4" />
                          Agendado para: {formatDate(reminder.scheduledFor)}
                        </span>
                        <span className="flex items-center gap-1">
                          <Clock className="w-4 h-4" />
                          Criado em: {formatDate(reminder.createdAt)}
                        </span>
                      </div>
                    </div>
                  </div>
                  <div className="flex gap-2">
                    <Button asChild variant="ghost" className="flex-1">
                      <Link href={`/automacao/lembretes/${reminder.reminderId}/editar`}>
                        <Edit className="w-4 h-4 mr-2" />
                        Editar
                      </Link>
                    </Button>
                    <Button
                      onClick={() => handleDeleteClick(reminder.reminderId, reminder.reason)}
                      variant="destructive"
                      className="flex-1"
                    >
                      <Trash2 className="w-4 h-4 mr-2" />
                      Excluir
                    </Button>
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

