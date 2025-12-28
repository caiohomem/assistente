"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { updateEmailTemplateClient, type EmailTemplate } from "@/lib/api/emailTemplatesApiClient";

interface EditarEmailTemplateClientProps {
  templateId: string;
  initialData: EmailTemplate;
}

export function EditarEmailTemplateClient({ templateId, initialData }: EditarEmailTemplateClientProps) {
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [formData, setFormData] = useState({
    name: initialData.name,
    subject: initialData.subject,
    htmlBody: initialData.htmlBody,
  });
  const [errors, setErrors] = useState<Record<string, string>>({});

  const validate = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!formData.name.trim()) {
      newErrors.name = "Nome é obrigatório";
    }

    if (!formData.subject.trim()) {
      newErrors.subject = "Assunto é obrigatório";
    }

    if (!formData.htmlBody.trim()) {
      newErrors.htmlBody = "Corpo HTML é obrigatório";
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
      await updateEmailTemplateClient(templateId, {
        name: formData.name.trim(),
        subject: formData.subject.trim(),
        htmlBody: formData.htmlBody.trim(),
      });

      router.push(`/email-templates/${templateId}`);
      router.refresh();
    } catch (error) {
      console.error("Erro ao atualizar template de email:", error);
      setErrors({
        submit: error instanceof Error ? error.message : "Erro ao atualizar template de email",
      });
    } finally {
      setLoading(false);
    }
  };

  const handleCancel = () => {
    router.push(`/email-templates/${templateId}`);
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {errors.submit && (
        <div className="rounded-md bg-red-50 dark:bg-red-900/20 p-4">
          <p className="text-sm text-red-800 dark:text-red-200">{errors.submit}</p>
        </div>
      )}

      {/* Informações do Template */}
      <div className="bg-zinc-50 dark:bg-zinc-900 rounded-md p-4 mb-4">
        <p className="text-sm text-zinc-600 dark:text-zinc-400">
          <strong>Tipo:</strong> {initialData.templateType === 1 ? "Usuário Criado" : initialData.templateType === 2 ? "Redefinição de Senha" : "Bem-vindo"}
        </p>
        <p className="text-sm text-zinc-600 dark:text-zinc-400 mt-1">
          <strong>Status:</strong> {initialData.isActive ? "Ativo" : "Inativo"}
        </p>
        {initialData.placeholders.length > 0 && (
          <p className="text-sm text-zinc-600 dark:text-zinc-400 mt-1">
            <strong>Placeholders detectados:</strong> {initialData.placeholders.join(", ")}
          </p>
        )}
      </div>

      {/* Nome */}
      <div>
        <label htmlFor="name" className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-2">
          Nome <span className="text-red-500">*</span>
        </label>
        <input
          type="text"
          id="name"
          value={formData.name}
          onChange={(e) => setFormData({ ...formData, name: e.target.value })}
          maxLength={200}
          className={`w-full rounded-md border ${
            errors.name
              ? "border-red-300 dark:border-red-700"
              : "border-zinc-300 dark:border-zinc-700"
          } bg-white dark:bg-zinc-800 text-zinc-900 dark:text-zinc-100 px-4 py-2 text-sm focus:border-indigo-500 dark:focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500`}
        />
        {errors.name && (
          <p className="mt-1 text-sm text-red-600 dark:text-red-400">{errors.name}</p>
        )}
      </div>

      {/* Assunto */}
      <div>
        <label htmlFor="subject" className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-2">
          Assunto <span className="text-red-500">*</span>
        </label>
        <input
          type="text"
          id="subject"
          value={formData.subject}
          onChange={(e) => setFormData({ ...formData, subject: e.target.value })}
          className={`w-full rounded-md border ${
            errors.subject
              ? "border-red-300 dark:border-red-700"
              : "border-zinc-300 dark:border-zinc-700"
          } bg-white dark:bg-zinc-800 text-zinc-900 dark:text-zinc-100 px-4 py-2 text-sm focus:border-indigo-500 dark:focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500`}
        />
        {errors.subject && (
          <p className="mt-1 text-sm text-red-600 dark:text-red-400">{errors.subject}</p>
        )}
      </div>

      {/* Corpo HTML */}
      <div>
        <label htmlFor="htmlBody" className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-2">
          Corpo HTML <span className="text-red-500">*</span>
        </label>
        <textarea
          id="htmlBody"
          value={formData.htmlBody}
          onChange={(e) => setFormData({ ...formData, htmlBody: e.target.value })}
          rows={20}
          className={`w-full rounded-md border ${
            errors.htmlBody
              ? "border-red-300 dark:border-red-700"
              : "border-zinc-300 dark:border-zinc-700"
          } bg-white dark:bg-zinc-800 text-zinc-900 dark:text-zinc-100 px-4 py-2 text-sm font-mono focus:border-indigo-500 dark:focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500`}
        />
        {errors.htmlBody && (
          <p className="mt-1 text-sm text-red-600 dark:text-red-400">{errors.htmlBody}</p>
        )}
      </div>

      {/* Botões */}
      <div className="flex justify-end gap-3 pt-4 border-t border-zinc-200 dark:border-zinc-700">
        <button
          type="button"
          onClick={handleCancel}
          className="rounded-md border border-zinc-300 dark:border-zinc-700 bg-white dark:bg-zinc-800 px-4 py-2 text-sm font-medium text-zinc-700 dark:text-zinc-300 hover:bg-zinc-50 dark:hover:bg-zinc-700"
        >
          Cancelar
        </button>
        <button
          type="submit"
          disabled={loading}
          className="rounded-md bg-indigo-600 dark:bg-indigo-500 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700 dark:hover:bg-indigo-600 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {loading ? "Atualizando..." : "Atualizar Template"}
        </button>
      </div>
    </form>
  );
}

