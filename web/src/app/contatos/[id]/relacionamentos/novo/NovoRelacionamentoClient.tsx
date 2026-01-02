"use client";

import { useState, useEffect } from "react";
import { useRouter } from "next/navigation";
import { useTranslations } from "next-intl";
import { addRelationshipClient } from "@/lib/api/contactsApiClient";
import { listContactsClient } from "@/lib/api/contactsApiClient";
import { Contact, AddContactRelationshipRequest } from "@/lib/types/contact";

interface NovoRelacionamentoClientProps {
  contactId: string;
}

const RELATIONSHIP_TYPES = [
  { value: "colleague", labelKey: "types.colleague" },
  { value: "friend", labelKey: "types.friend" },
  { value: "family", labelKey: "types.family" },
  { value: "client", labelKey: "types.client" },
  { value: "supplier", labelKey: "types.supplier" },
  { value: "partner", labelKey: "types.partner" },
  { value: "other", labelKey: "types.other" },
];

export function NovoRelacionamentoClient({ contactId }: NovoRelacionamentoClientProps) {
  const router = useRouter();
  const t = useTranslations();
  const tRel = useTranslations("relationships");

  const [loading, setLoading] = useState(false);
  const [loadingContacts, setLoadingContacts] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [contacts, setContacts] = useState<Contact[]>([]);
  
  const [formData, setFormData] = useState<AddContactRelationshipRequest>({
    targetContactId: "",
    type: "colleague",
    description: null,
    strength: 0.5,
    isConfirmed: false,
  });

  useEffect(() => {
    async function loadContacts() {
      try {
        setLoadingContacts(true);
        const result = await listContactsClient({ page: 1, pageSize: 1000 });
        // Filtrar o contato atual
        const filtered = result.contacts.filter((c) => c.contactId !== contactId);
        setContacts(filtered);
      } catch (err) {
        setError(err instanceof Error ? err.message : t("errors.generic"));
      } finally {
        setLoadingContacts(false);
      }
    }
    loadContacts();
  }, [contactId, t]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    // Validações
    if (!contactId || contactId.trim() === "") {
      setError("ID do contato de origem inválido");
      return;
    }

    // Validar formato do GUID do sourceContactId
    const guidRegex = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
    if (!guidRegex.test(contactId)) {
      setError("ID do contato de origem está em formato inválido. Por favor, recarregue a página.");
      return;
    }

    if (!formData.targetContactId || formData.targetContactId.trim() === "") {
      setError(tRel("selectTargetContact"));
      return;
    }

    if (!guidRegex.test(formData.targetContactId)) {
      setError("ID do contato de destino está em formato inválido.");
      return;
    }

    if (formData.targetContactId === contactId) {
      setError(tRel("cannotRelateToSelf"));
      return;
    }

    if (!formData.type || formData.type.trim() === "") {
      setError("Tipo de relacionamento é obrigatório");
      return;
    }

    setLoading(true);
    try {
      // O sourceContactId (contactId) é passado na URL: /api/contacts/{contactId}/relationships
      // O targetContactId vem no body do request
      await addRelationshipClient(contactId, formData);
      router.push(`/contatos/${contactId}`);
      router.refresh();
    } catch (error: unknown) {
      const errorMessage =
        error instanceof Error && error.message ? error.message : tRel("errorCreating");
      if (errorMessage.includes("duplicate") || errorMessage.includes("já existe")) {
        setError(tRel("duplicateRelationship"));
      } else if (errorMessage.includes("não encontrado") || errorMessage.includes("not found")) {
        // Melhorar mensagem de erro para indicar qual contato não foi encontrado
        if (errorMessage.includes("destino") || errorMessage.includes("target")) {
          setError("O contato de destino selecionado não foi encontrado. Por favor, selecione outro contato.");
        } else {
          setError("Contato não encontrado. Por favor, recarregue a página e tente novamente.");
        }
      } else {
        setError(errorMessage);
      }
    } finally {
      setLoading(false);
    }
  };

  const handleCancel = () => {
    router.push(`/contatos/${contactId}`);
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {error && (
        <div className="rounded-md bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 p-4">
          <p className="text-sm text-red-800 dark:text-red-400">{error}</p>
        </div>
      )}

      {/* Target Contact */}
      <div>
        <label className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-2">
          {tRel("targetContact")} <span className="text-red-500">*</span>
        </label>
        {loadingContacts ? (
          <div className="text-sm text-zinc-500 dark:text-zinc-400">{t("common.loading")}</div>
        ) : (
          <select
            value={formData.targetContactId}
            onChange={(e) => setFormData({ ...formData, targetContactId: e.target.value })}
            className="w-full px-3 py-2 border border-zinc-300 dark:border-zinc-600 rounded-md bg-white dark:bg-zinc-700 text-zinc-900 dark:text-zinc-100 focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
            required
          >
            <option value="">{tRel("selectTargetContact")}</option>
            {contacts.map((contact) => (
              <option key={contact.contactId} value={contact.contactId}>
                {contact.fullName} {contact.company ? `(${contact.company})` : ""}
              </option>
            ))}
          </select>
        )}
        {contacts.length === 0 && !loadingContacts && (
          <p className="mt-1 text-xs text-zinc-500 dark:text-zinc-400">
            {t("contacts.noContacts")}
          </p>
        )}
      </div>

      {/* Type */}
      <div>
        <label className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-2">
          {tRel("type")} <span className="text-red-500">*</span>
        </label>
        <select
          value={formData.type}
          onChange={(e) => setFormData({ ...formData, type: e.target.value })}
          className="w-full px-3 py-2 border border-zinc-300 dark:border-zinc-600 rounded-md bg-white dark:bg-zinc-700 text-zinc-900 dark:text-zinc-100 focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
          required
        >
          {RELATIONSHIP_TYPES.map((type) => (
            <option key={type.value} value={type.value}>
              {tRel(type.labelKey)}
            </option>
          ))}
        </select>
      </div>

      {/* Description */}
      <div>
        <label className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-2">
          {tRel("description")} <span className="text-zinc-400 text-xs">({t("common.optional")})</span>
        </label>
        <textarea
          value={formData.description || ""}
          onChange={(e) => setFormData({ ...formData, description: e.target.value || null })}
          rows={3}
          className="w-full px-3 py-2 border border-zinc-300 dark:border-zinc-600 rounded-md bg-white dark:bg-zinc-700 text-zinc-900 dark:text-zinc-100 focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
          placeholder={tRel("descriptionPlaceholder")}
        />
      </div>

      {/* Strength */}
      <div>
        <label className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-2">
          {tRel("strength")}: {Math.round((formData.strength || 0) * 100)}%
        </label>
        <input
          type="range"
          min="0"
          max="1"
          step="0.01"
          value={formData.strength || 0.5}
          onChange={(e) => setFormData({ ...formData, strength: parseFloat(e.target.value) })}
          className="w-full h-2 bg-zinc-200 dark:bg-zinc-700 rounded-lg appearance-none cursor-pointer"
        />
        <p className="mt-1 text-xs text-zinc-500 dark:text-zinc-400">
          {tRel("strengthDescription")}
        </p>
      </div>

      {/* Is Confirmed */}
      <div className="flex items-center">
        <input
          type="checkbox"
          id="isConfirmed"
          checked={formData.isConfirmed || false}
          onChange={(e) => setFormData({ ...formData, isConfirmed: e.target.checked })}
          className="h-4 w-4 text-indigo-600 focus:ring-indigo-500 border-zinc-300 dark:border-zinc-600 rounded"
        />
        <label htmlFor="isConfirmed" className="ml-2 block text-sm text-zinc-700 dark:text-zinc-300">
          {tRel("isConfirmed")}
        </label>
      </div>

      {/* Actions */}
      <div className="flex gap-4 pt-4">
        <button
          type="submit"
          disabled={loading || loadingContacts}
          className="flex-1 rounded-md bg-zinc-900 dark:bg-zinc-800 px-4 py-2 text-sm font-medium text-white hover:bg-zinc-800 dark:hover:bg-zinc-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
        >
          {loading ? tRel("creating") : tRel("create")}
        </button>
        <button
          type="button"
          onClick={handleCancel}
          disabled={loading}
          className="flex-1 rounded-md border border-zinc-300 dark:border-zinc-600 bg-white dark:bg-zinc-800 px-4 py-2 text-sm font-medium text-zinc-700 dark:text-zinc-300 hover:bg-zinc-50 dark:hover:bg-zinc-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
        >
          {t("common.cancel")}
        </button>
      </div>
    </form>
  );
}
