"use client";

import { useState } from "react";
import { createTemplateClient } from "@/lib/api/automationApiClient";
import { TemplateType } from "@/lib/types/automation";

interface NovoTemplateClientProps {
  onSuccess?: () => void;
  onCancel?: () => void;
}

export function NovoTemplateClient({ onSuccess, onCancel }: NovoTemplateClientProps) {
  const [loading, setLoading] = useState(false);
  const [formData, setFormData] = useState({
    name: "",
    type: TemplateType.Email.toString(),
    body: "",
    placeholdersSchema: "{}",
  });
  const [errors, setErrors] = useState<Record<string, string>>({});

  const validate = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!formData.name.trim()) {
      newErrors.name = "Nome é obrigatório";
    }

    if (!formData.body.trim()) {
      newErrors.body = "Corpo do template é obrigatório";
    }

    // Validar JSON do schema se fornecido
    if (formData.placeholdersSchema && formData.placeholdersSchema.trim()) {
      try {
        JSON.parse(formData.placeholdersSchema);
      } catch {
        newErrors.placeholdersSchema = "Schema de placeholders deve ser um JSON válido";
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
      await createTemplateClient({
        name: formData.name.trim(),
        type: parseInt(formData.type) as TemplateType,
        body: formData.body.trim(),
        placeholdersSchema: formData.placeholdersSchema.trim() || null,
      });

      if (onSuccess) {
        onSuccess();
      }
    } catch (error) {
      console.error("Erro ao criar template:", error);
      setErrors({
        submit: error instanceof Error ? error.message : "Erro ao criar template",
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

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
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
              placeholder="Ex: Template de E-mail de Boas-vindas"
            />
            {errors.name && (
              <p className="mt-1 text-sm text-red-600 dark:text-red-400">{errors.name}</p>
            )}
          </div>

          {/* Tipo */}
          <div>
            <label htmlFor="type" className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-2">
              Tipo <span className="text-red-500">*</span>
            </label>
            <select
              id="type"
              value={formData.type}
              onChange={(e) => setFormData({ ...formData, type: e.target.value })}
              className="w-full rounded-md border border-zinc-300 dark:border-zinc-700 bg-white dark:bg-zinc-800 text-zinc-900 dark:text-zinc-100 px-4 py-2 text-sm focus:border-indigo-500 dark:focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500"
            >
              <option value={TemplateType.Email.toString()}>E-mail</option>
              <option value={TemplateType.Oficio.toString()}>Ofício</option>
              <option value={TemplateType.Invite.toString()}>Convite</option>
              <option value={TemplateType.Generic.toString()}>Genérico</option>
            </select>
          </div>

          {/* Corpo */}
          <div>
            <label htmlFor="body" className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-2">
              Corpo do Template <span className="text-red-500">*</span>
            </label>
            <textarea
              id="body"
              value={formData.body}
              onChange={(e) => setFormData({ ...formData, body: e.target.value })}
              rows={12}
              className={`w-full rounded-md border ${
                errors.body
                  ? "border-red-300 dark:border-red-700"
                  : "border-zinc-300 dark:border-zinc-700"
              } bg-white dark:bg-zinc-800 text-zinc-900 dark:text-zinc-100 px-4 py-2 text-sm focus:border-indigo-500 dark:focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500 font-mono`}
              placeholder="Digite o conteúdo do template. Use {{placeholder}} para variáveis."
            />
            {errors.body && (
              <p className="mt-1 text-sm text-red-600 dark:text-red-400">{errors.body}</p>
            )}
          </div>

          {/* Schema de Placeholders */}
          <div>
            <label htmlFor="placeholdersSchema" className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-2">
              Schema de Placeholders (JSON)
            </label>
            <textarea
              id="placeholdersSchema"
              value={formData.placeholdersSchema}
              onChange={(e) => setFormData({ ...formData, placeholdersSchema: e.target.value })}
              rows={6}
              className={`w-full rounded-md border ${
                errors.placeholdersSchema
                  ? "border-red-300 dark:border-red-700"
                  : "border-zinc-300 dark:border-zinc-700"
              } bg-white dark:bg-zinc-800 text-zinc-900 dark:text-zinc-100 px-4 py-2 text-sm focus:border-indigo-500 dark:focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500 font-mono`}
              placeholder='{"name": "string", "email": "string"}'
            />
            {errors.placeholdersSchema && (
              <p className="mt-1 text-sm text-red-600 dark:text-red-400">{errors.placeholdersSchema}</p>
            )}
            <p className="mt-1 text-xs text-zinc-500 dark:text-zinc-400">
              Defina o schema JSON dos placeholders que podem ser usados no template.
            </p>
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
              {loading ? "Criando..." : "Criar Template"}
            </button>
          </div>
    </form>
  );
}





