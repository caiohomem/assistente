"use client";

import { useState, useEffect } from "react";
import { SearchableContactSelect } from "@/components/SearchableContactSelect";
import { createReminderClient } from "@/lib/api/automationApiClient";
import { listContactsClient } from "@/lib/api/contactsApiClient";
import type { Contact } from "@/lib/types/contact";

interface NovoLembreteClientProps {
  onSuccess?: () => void;
  onCancel?: () => void;
}

export function NovoLembreteClient({ onSuccess, onCancel }: NovoLembreteClientProps) {
  const [loading, setLoading] = useState(false);
  const [contacts, setContacts] = useState<Contact[]>([]);
  const [loadingContacts, setLoadingContacts] = useState(true);
  const [formData, setFormData] = useState({
    contactId: "",
    reason: "",
    suggestedMessage: "",
    scheduledFor: "",
  });
  const [errors, setErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    loadContacts();
  }, []);

  const loadContacts = async () => {
    try {
      const result = await listContactsClient({ page: 1, pageSize: 100 });
      setContacts(result.contacts);
    } catch (err) {
      console.error("Erro ao carregar contatos:", err);
    } finally {
      setLoadingContacts(false);
    }
  };

  const validate = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!formData.contactId) {
      newErrors.contactId = "Contato é obrigatório";
    }

    if (!formData.reason.trim()) {
      newErrors.reason = "Motivo é obrigatório";
    }

    if (!formData.scheduledFor) {
      newErrors.scheduledFor = "Data agendada é obrigatória";
    } else {
      const scheduledDate = new Date(formData.scheduledFor);
      if (scheduledDate < new Date()) {
        newErrors.scheduledFor = "A data deve ser futura";
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
      await createReminderClient({
        contactId: formData.contactId,
        reason: formData.reason.trim(),
        suggestedMessage: formData.suggestedMessage.trim() || null,
        scheduledFor: new Date(formData.scheduledFor).toISOString(),
      });

      if (onSuccess) {
        onSuccess();
      }
    } catch (error) {
      console.error("Erro ao criar lembrete:", error);
      setErrors({
        submit: error instanceof Error ? error.message : "Erro ao criar lembrete",
      });
    } finally {
      setLoading(false);
    }
  };

  const handleCancel = () => {
    if (onCancel) {
      onCancel();
    }
  };

  // Get minimum date (today)
  const minDate = new Date().toISOString().slice(0, 16);

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
          {/* Contato */}
          <div>
            <label htmlFor="contactId" className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-2">
              Contato <span className="text-red-500">*</span>
            </label>
            <SearchableContactSelect
              contacts={contacts}
              value={formData.contactId}
              onChange={(contactId) => setFormData({ ...formData, contactId })}
              loading={loadingContacts}
              error={errors.contactId}
              placeholder="Selecione um contato"
            />
          </div>

          {/* Motivo */}
          <div>
            <label htmlFor="reason" className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-2">
              Motivo <span className="text-red-500">*</span>
            </label>
            <input
              type="text"
              id="reason"
              value={formData.reason}
              onChange={(e) => setFormData({ ...formData, reason: e.target.value })}
              maxLength={500}
              className={`w-full rounded-md border ${
                errors.reason
                  ? "border-red-300 dark:border-red-700"
                  : "border-zinc-300 dark:border-zinc-700"
              } bg-white dark:bg-zinc-800 text-zinc-900 dark:text-zinc-100 px-4 py-2 text-sm focus:border-indigo-500 dark:focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500`}
              placeholder="Ex: Seguir com reunião de follow-up"
            />
            {errors.reason && (
              <p className="mt-1 text-sm text-red-600 dark:text-red-400">{errors.reason}</p>
            )}
          </div>

          {/* Mensagem Sugerida */}
          <div>
            <label htmlFor="suggestedMessage" className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-2">
              Mensagem Sugerida
            </label>
            <textarea
              id="suggestedMessage"
              value={formData.suggestedMessage}
              onChange={(e) => setFormData({ ...formData, suggestedMessage: e.target.value })}
              maxLength={2000}
              rows={4}
              className="w-full rounded-md border border-zinc-300 dark:border-zinc-700 bg-white dark:bg-zinc-800 text-zinc-900 dark:text-zinc-100 px-4 py-2 text-sm focus:border-indigo-500 dark:focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500"
              placeholder="Mensagem que será sugerida quando o lembrete for enviado"
            />
          </div>

          {/* Data Agendada */}
          <div>
            <label htmlFor="scheduledFor" className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-2">
              Data e Hora Agendada <span className="text-red-500">*</span>
            </label>
            <input
              type="datetime-local"
              id="scheduledFor"
              value={formData.scheduledFor}
              onChange={(e) => setFormData({ ...formData, scheduledFor: e.target.value })}
              min={minDate}
              className={`w-full rounded-md border ${
                errors.scheduledFor
                  ? "border-red-300 dark:border-red-700"
                  : "border-zinc-300 dark:border-zinc-700"
              } bg-white dark:bg-zinc-800 text-zinc-900 dark:text-zinc-100 px-4 py-2 text-sm focus:border-indigo-500 dark:focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500`}
            />
            {errors.scheduledFor && (
              <p className="mt-1 text-sm text-red-600 dark:text-red-400">{errors.scheduledFor}</p>
            )}
          </div>

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
              {loading ? "Criando..." : "Criar Lembrete"}
            </button>
          </div>
    </form>
  );
}

