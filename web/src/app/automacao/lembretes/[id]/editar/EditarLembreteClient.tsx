"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { updateReminderStatusClient } from "@/lib/api/automationApiClient";
import { ReminderStatus } from "@/lib/types/automation";

interface EditarLembreteClientProps {
  reminderId: string;
  initialData: {
    contactId: string;
    reason: string;
    suggestedMessage: string;
    scheduledFor: string;
    status: ReminderStatus;
  };
}

export function EditarLembreteClient({ reminderId, initialData }: EditarLembreteClientProps) {
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [formData, setFormData] = useState({
    status: initialData.status.toString(),
    newScheduledFor: "",
  });
  const [errors, setErrors] = useState<Record<string, string>>({});

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

  const validate = (): boolean => {
    const newErrors: Record<string, string> = {};

    const newStatus = parseInt(formData.status) as ReminderStatus;
    
    // Se for adiar (Snoozed), precisa de uma nova data
    if (newStatus === ReminderStatus.Snoozed && !formData.newScheduledFor) {
      newErrors.newScheduledFor = "Data agendada é obrigatória ao adiar o lembrete";
    }

    if (formData.newScheduledFor) {
      const scheduledDate = new Date(formData.newScheduledFor);
      if (scheduledDate < new Date()) {
        newErrors.newScheduledFor = "A data deve ser futura";
      }
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validate()) {
      return;
    }

    setLoading(true);
    try {
      const newStatus = parseInt(formData.status) as ReminderStatus;
      await updateReminderStatusClient(reminderId, {
        newStatus,
        newScheduledFor: formData.newScheduledFor ? new Date(formData.newScheduledFor).toISOString() : null,
      });

      router.push(`/automacao/lembretes`);
      router.refresh();
    } catch (error) {
      console.error("Erro ao atualizar lembrete:", error);
      setErrors({
        submit: error instanceof Error ? error.message : "Erro ao atualizar lembrete",
      });
    } finally {
      setLoading(false);
    }
  };

  const handleCancel = () => {
    router.push(`/automacao/lembretes`);
  };

  const minDate = new Date().toISOString().slice(0, 16);
  const showSnoozeDate = parseInt(formData.status) === ReminderStatus.Snoozed;

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {/* Informações do Lembrete (read-only) */}
      <div className="rounded-md bg-zinc-50 dark:bg-zinc-900/50 border border-zinc-200 dark:border-zinc-700 p-4 space-y-3">
        <div>
          <label className="block text-xs font-medium text-zinc-500 dark:text-zinc-400 mb-1">
            Motivo
          </label>
          <p className="text-sm text-zinc-900 dark:text-zinc-100">{initialData.reason}</p>
        </div>
        
        {initialData.suggestedMessage && (
          <div>
            <label className="block text-xs font-medium text-zinc-500 dark:text-zinc-400 mb-1">
              Mensagem Sugerida
            </label>
            <p className="text-sm text-zinc-600 dark:text-zinc-400">{initialData.suggestedMessage}</p>
          </div>
        )}
        
        <div>
          <label className="block text-xs font-medium text-zinc-500 dark:text-zinc-400 mb-1">
            Data Agendada Original
          </label>
          <p className="text-sm text-zinc-900 dark:text-zinc-100">{formatDate(initialData.scheduledFor)}</p>
        </div>
        
        <div>
          <label className="block text-xs font-medium text-zinc-500 dark:text-zinc-400 mb-1">
            Status Atual
          </label>
          <p className="text-sm text-zinc-900 dark:text-zinc-100">{getStatusLabel(initialData.status)}</p>
        </div>
      </div>

      <div className="rounded-md bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 p-4">
        <p className="text-sm text-yellow-800 dark:text-yellow-200">
          <strong>Nota:</strong> Apenas o status do lembrete pode ser alterado. O motivo, mensagem sugerida e contato não podem ser editados após a criação.
        </p>
      </div>

      {/* Status */}
      <div>
        <label htmlFor="status" className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-2">
          Novo Status <span className="text-red-500">*</span>
        </label>
        <select
          id="status"
          value={formData.status}
          onChange={(e) => setFormData({ ...formData, status: e.target.value, newScheduledFor: "" })}
          className="w-full rounded-md border border-zinc-300 dark:border-zinc-700 bg-white dark:bg-zinc-800 text-zinc-900 dark:text-zinc-100 px-4 py-2 text-sm focus:border-indigo-500 dark:focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500"
        >
          <option value={ReminderStatus.Pending.toString()}>Pendente</option>
          <option value={ReminderStatus.Sent.toString()}>Enviado</option>
          <option value={ReminderStatus.Dismissed.toString()}>Dispensado</option>
          <option value={ReminderStatus.Snoozed.toString()}>Adiado</option>
        </select>
        <p className="mt-1 text-xs text-zinc-500 dark:text-zinc-400">
          Selecione o novo status do lembrete.
        </p>
      </div>

      {/* Nova Data Agendada (apenas para Snoozed) */}
      {showSnoozeDate && (
        <div>
          <label htmlFor="newScheduledFor" className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-2">
            Nova Data e Hora Agendada <span className="text-red-500">*</span>
          </label>
          <input
            type="datetime-local"
            id="newScheduledFor"
            value={formData.newScheduledFor}
            onChange={(e) => setFormData({ ...formData, newScheduledFor: e.target.value })}
            min={minDate}
            className={`w-full rounded-md border ${
              errors.newScheduledFor
                ? "border-red-300 dark:border-red-700"
                : "border-zinc-300 dark:border-zinc-700"
            } bg-white dark:bg-zinc-800 text-zinc-900 dark:text-zinc-100 px-4 py-2 text-sm focus:border-indigo-500 dark:focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500`}
          />
          {errors.newScheduledFor && (
            <p className="mt-1 text-sm text-red-600 dark:text-red-400">{errors.newScheduledFor}</p>
          )}
          <p className="mt-1 text-xs text-zinc-500 dark:text-zinc-400">
            Defina a nova data e hora para quando o lembrete deve ser exibido novamente.
          </p>
        </div>
      )}

      {/* Erro geral */}
      {errors.submit && (
        <div className="rounded-md bg-red-50 dark:bg-red-900/20 p-4">
          <p className="text-sm text-red-800 dark:text-red-200">{errors.submit}</p>
        </div>
      )}

      {/* Botões */}
      <div className="flex gap-4 justify-end">
        <button
          type="button"
          onClick={handleCancel}
          className="rounded-md border border-zinc-300 dark:border-zinc-700 bg-white dark:bg-zinc-800 px-4 py-2 text-sm font-medium text-zinc-700 dark:text-zinc-300 hover:bg-zinc-50 dark:hover:bg-zinc-700 focus:outline-none focus:ring-2 focus:ring-indigo-500"
        >
          Cancelar
        </button>
        <button
          type="submit"
          disabled={loading}
          className="rounded-md bg-indigo-600 dark:bg-indigo-500 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700 dark:hover:bg-indigo-600 focus:outline-none focus:ring-2 focus:ring-indigo-500 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {loading ? "Salvando..." : "Salvar Alterações"}
        </button>
      </div>
    </form>
  );
}





