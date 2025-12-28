"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { createEmailTemplateClient } from "@/lib/api/emailTemplatesApiClient";
import { EmailTemplateType } from "@/lib/types/emailTemplates";

export function NovoEmailTemplateClient() {
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [formData, setFormData] = useState({
    name: "",
    templateType: EmailTemplateType.UserCreated.toString(),
    subject: "",
    htmlBody: "",
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
      const result = await createEmailTemplateClient({
        name: formData.name.trim(),
        templateType: parseInt(formData.templateType) as EmailTemplateType,
        subject: formData.subject.trim(),
        htmlBody: formData.htmlBody.trim(),
      });

      router.push(`/email-templates/${result.id}`);
      router.refresh();
    } catch (error) {
      console.error("Erro ao criar template de email:", error);
      setErrors({
        submit: error instanceof Error ? error.message : "Erro ao criar template de email",
      });
    } finally {
      setLoading(false);
    }
  };

  const handleCancel = () => {
    router.push("/email-templates");
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {errors.submit && (
        <div className="rounded-md bg-red-50 dark:bg-red-900/20 p-4">
          <p className="text-sm text-red-800 dark:text-red-200">{errors.submit}</p>
        </div>
      )}

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
          placeholder="Ex: Template de Boas-vindas"
        />
        {errors.name && (
          <p className="mt-1 text-sm text-red-600 dark:text-red-400">{errors.name}</p>
        )}
      </div>

      {/* Tipo */}
      <div>
        <label htmlFor="templateType" className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-2">
          Tipo <span className="text-red-500">*</span>
        </label>
        <select
          id="templateType"
          value={formData.templateType}
          onChange={(e) => setFormData({ ...formData, templateType: e.target.value })}
          className={`w-full rounded-md border ${
            errors.templateType
              ? "border-red-300 dark:border-red-700"
              : "border-zinc-300 dark:border-zinc-700"
          } bg-white dark:bg-zinc-800 text-zinc-900 dark:text-zinc-100 px-4 py-2 text-sm focus:border-indigo-500 dark:focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500`}
        >
          <option value={EmailTemplateType.UserCreated}>Usuário Criado</option>
          <option value={EmailTemplateType.PasswordReset}>Redefinição de Senha</option>
          <option value={EmailTemplateType.Welcome}>Bem-vindo</option>
        </select>
        {errors.templateType && (
          <p className="mt-1 text-sm text-red-600 dark:text-red-400">{errors.templateType}</p>
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
          placeholder="Ex: Bem-vindo ao Assistente Executivo, {{ NomeUsuario }}!"
        />
        {errors.subject && (
          <p className="mt-1 text-sm text-red-600 dark:text-red-400">{errors.subject}</p>
        )}
        <p className="mt-1 text-xs text-zinc-500 dark:text-zinc-400">
          Use placeholders como {"{{ NomeUsuario }}"} para personalização
        </p>
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
          placeholder="<!DOCTYPE html>..."
        />
        {errors.htmlBody && (
          <p className="mt-1 text-sm text-red-600 dark:text-red-400">{errors.htmlBody}</p>
        )}
        <p className="mt-1 text-xs text-zinc-500 dark:text-zinc-400">
          Use placeholders como {"{{ NomeUsuario }}"} para personalização
        </p>
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
          {loading ? "Criando..." : "Criar Template"}
        </button>
      </div>
    </form>
  );
}

