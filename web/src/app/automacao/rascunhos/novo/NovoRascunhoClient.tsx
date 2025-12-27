"use client";

import { useState, useEffect } from "react";
import { useRouter } from "next/navigation";
import { TopBar } from "@/components/TopBar";
import { createDraftClient, listTemplatesClient, listLetterheadsClient } from "@/lib/api/automationApiClient";
import { listContactsClient } from "@/lib/api/contactsApiClient";
import type { Contact } from "@/lib/types/contact";
import type { Template, Letterhead } from "@/lib/types/automation";
import { DocumentType } from "@/lib/types/automation";

export function NovoRascunhoClient() {
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [contacts, setContacts] = useState<Contact[]>([]);
  const [templates, setTemplates] = useState<Template[]>([]);
  const [letterheads, setLetterheads] = useState<Letterhead[]>([]);
  const [loadingData, setLoadingData] = useState(true);
  const [formData, setFormData] = useState({
    documentType: DocumentType.Email.toString(),
    content: "",
    contactId: "",
    templateId: "",
    letterheadId: "",
  });
  const [errors, setErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      const [contactsResult, templatesResult, letterheadsResult] = await Promise.all([
        listContactsClient({ page: 1, pageSize: 100 }),
        listTemplatesClient({ activeOnly: true, page: 1, pageSize: 100 }),
        listLetterheadsClient({ activeOnly: true, page: 1, pageSize: 100 }),
      ]);
      setContacts(contactsResult.contacts);
      setTemplates(templatesResult.templates);
      setLetterheads(letterheadsResult.letterheads);
    } catch (err) {
      console.error("Erro ao carregar dados:", err);
    } finally {
      setLoadingData(false);
    }
  };

  const validate = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!formData.content.trim()) {
      newErrors.content = "Conteúdo é obrigatório";
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
      await createDraftClient({
        documentType: parseInt(formData.documentType) as DocumentType,
        content: formData.content.trim(),
        contactId: formData.contactId || null,
        templateId: formData.templateId || null,
        letterheadId: formData.letterheadId || null,
      });

      router.push("/automacao/rascunhos");
      router.refresh();
    } catch (error) {
      console.error("Erro ao criar rascunho:", error);
      setErrors({
        submit: error instanceof Error ? error.message : "Erro ao criar rascunho",
      });
    } finally {
      setLoading(false);
    }
  };

  const handleCancel = () => {
    router.push("/automacao/rascunhos");
  };


  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-900">
      <TopBar title="Novo Rascunho" showBackButton backHref="/automacao/rascunhos" />
      <div className="mx-auto max-w-4xl px-4 py-8 sm:px-6 lg:px-8">
        <form onSubmit={handleSubmit} className="space-y-6">
          {/* Tipo de Documento */}
          <div>
            <label htmlFor="documentType" className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-2">
              Tipo de Documento <span className="text-red-500">*</span>
            </label>
            <select
              id="documentType"
              value={formData.documentType}
              onChange={(e) => setFormData({ ...formData, documentType: e.target.value })}
              className="w-full rounded-md border border-zinc-300 dark:border-zinc-700 bg-white dark:bg-zinc-800 text-zinc-900 dark:text-zinc-100 px-4 py-2 text-sm focus:border-indigo-500 dark:focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500"
            >
              <option value={DocumentType.Email.toString()}>E-mail</option>
              <option value={DocumentType.Oficio.toString()}>Ofício</option>
              <option value={DocumentType.Invite.toString()}>Convite</option>
            </select>
          </div>

          {/* Contato (opcional) */}
          <div>
            <label htmlFor="contactId" className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-2">
              Contato
            </label>
            {loadingData ? (
              <div className="text-sm text-zinc-500">Carregando contatos...</div>
            ) : (
              <select
                id="contactId"
                value={formData.contactId}
                onChange={(e) => setFormData({ ...formData, contactId: e.target.value })}
                className="w-full rounded-md border border-zinc-300 dark:border-zinc-700 bg-white dark:bg-zinc-800 text-zinc-900 dark:text-zinc-100 px-4 py-2 text-sm focus:border-indigo-500 dark:focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500"
              >
                <option value="">Nenhum contato</option>
                {contacts.map((contact) => (
                  <option key={contact.contactId} value={contact.contactId}>
                    {contact.fullName}
                  </option>
                ))}
              </select>
            )}
          </div>

          {/* Template (opcional) */}
          <div>
            <label htmlFor="templateId" className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-2">
              Template
            </label>
            {loadingData ? (
              <div className="text-sm text-zinc-500">Carregando templates...</div>
            ) : (
              <select
                id="templateId"
                value={formData.templateId}
                onChange={(e) => setFormData({ ...formData, templateId: e.target.value })}
                className="w-full rounded-md border border-zinc-300 dark:border-zinc-700 bg-white dark:bg-zinc-800 text-zinc-900 dark:text-zinc-100 px-4 py-2 text-sm focus:border-indigo-500 dark:focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500"
              >
                <option value="">Nenhum template</option>
                {templates.map((template) => (
                  <option key={template.templateId} value={template.templateId}>
                    {template.name}
                  </option>
                ))}
              </select>
            )}
          </div>

          {/* Papel Timbrado (opcional) */}
          <div>
            <label htmlFor="letterheadId" className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-2">
              Papel Timbrado
            </label>
            {loadingData ? (
              <div className="text-sm text-zinc-500">Carregando papéis timbrados...</div>
            ) : (
              <select
                id="letterheadId"
                value={formData.letterheadId}
                onChange={(e) => setFormData({ ...formData, letterheadId: e.target.value })}
                className="w-full rounded-md border border-zinc-300 dark:border-zinc-700 bg-white dark:bg-zinc-800 text-zinc-900 dark:text-zinc-100 px-4 py-2 text-sm focus:border-indigo-500 dark:focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500"
              >
                <option value="">Nenhum papel timbrado</option>
                {letterheads.map((letterhead) => (
                  <option key={letterhead.letterheadId} value={letterhead.letterheadId}>
                    {letterhead.name}
                  </option>
                ))}
              </select>
            )}
          </div>

          {/* Conteúdo */}
          <div>
            <label htmlFor="content" className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-2">
              Conteúdo <span className="text-red-500">*</span>
            </label>
            <textarea
              id="content"
              value={formData.content}
              onChange={(e) => setFormData({ ...formData, content: e.target.value })}
              rows={12}
              className={`w-full rounded-md border ${
                errors.content
                  ? "border-red-300 dark:border-red-700"
                  : "border-zinc-300 dark:border-zinc-700"
              } bg-white dark:bg-zinc-800 text-zinc-900 dark:text-zinc-100 px-4 py-2 text-sm focus:border-indigo-500 dark:focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500 font-mono`}
              placeholder="Digite o conteúdo do documento..."
            />
            {errors.content && (
              <p className="mt-1 text-sm text-red-600 dark:text-red-400">{errors.content}</p>
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
              {loading ? "Criando..." : "Criar Rascunho"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

