"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { updateLetterheadClient } from "@/lib/api/automationApiClient";

interface EditarPapelTimbradoClientProps {
  letterheadId: string;
  initialData: {
    name: string;
    designData: string;
    isActive: boolean;
  };
}

export function EditarPapelTimbradoClient({ letterheadId, initialData }: EditarPapelTimbradoClientProps) {
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [formData, setFormData] = useState({
    name: initialData.name,
    designData: initialData.designData,
    isActive: initialData.isActive,
  });
  const [errors, setErrors] = useState<Record<string, string>>({});

  const validate = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!formData.name.trim()) {
      newErrors.name = "Nome é obrigatório";
    }

    // Validar JSON do designData
    if (!formData.designData || !formData.designData.trim()) {
      newErrors.designData = "Dados de design são obrigatórios";
    } else {
      try {
        JSON.parse(formData.designData);
      } catch {
        newErrors.designData = "Dados de design devem ser um JSON válido";
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
      await updateLetterheadClient(letterheadId, {
        name: formData.name.trim(),
        designData: formData.designData.trim(),
        isActive: formData.isActive,
      });

      router.push(`/automacao/papeis-timbrados/${letterheadId}`);
      router.refresh();
    } catch (error) {
      console.error("Erro ao atualizar papel timbrado:", error);
      setErrors({
        submit: error instanceof Error ? error.message : "Erro ao atualizar papel timbrado",
      });
    } finally {
      setLoading(false);
    }
  };

  const handleCancel = () => {
    router.push(`/automacao/papeis-timbrados/${letterheadId}`);
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
          placeholder="Ex: Papel Timbrado Empresarial"
        />
        {errors.name && (
          <p className="mt-1 text-sm text-red-600 dark:text-red-400">{errors.name}</p>
        )}
      </div>

      {/* Dados de Design */}
      <div>
        <label htmlFor="designData" className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-2">
          Dados de Design (JSON) <span className="text-red-500">*</span>
        </label>
        <textarea
          id="designData"
          value={formData.designData}
          onChange={(e) => setFormData({ ...formData, designData: e.target.value })}
          rows={12}
          className={`w-full rounded-md border ${
            errors.designData
              ? "border-red-300 dark:border-red-700"
              : "border-zinc-300 dark:border-zinc-700"
          } bg-white dark:bg-zinc-800 text-zinc-900 dark:text-zinc-100 px-4 py-2 text-sm focus:border-indigo-500 dark:focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500 font-mono`}
          placeholder='{"header": "...", "footer": "...", "logo": "..."}'
        />
        {errors.designData && (
          <p className="mt-1 text-sm text-red-600 dark:text-red-400">{errors.designData}</p>
        )}
        <p className="mt-1 text-xs text-zinc-500 dark:text-zinc-400">
          Defina os dados de design do papel timbrado em formato JSON. Inclua informações sobre cabeçalho, rodapé, logo, etc.
        </p>
      </div>

      {/* Ativo */}
      <div>
        <label className="flex items-center gap-2">
          <input
            type="checkbox"
            checked={formData.isActive}
            onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
            className="rounded border-zinc-300 dark:border-zinc-700 text-indigo-600 focus:ring-indigo-500"
          />
          <span className="text-sm font-medium text-zinc-700 dark:text-zinc-300">
            Papel timbrado ativo
          </span>
        </label>
        <p className="mt-1 text-xs text-zinc-500 dark:text-zinc-400">
          Apenas papéis timbrados ativos podem ser usados na criação de documentos.
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
          {loading ? "Salvando..." : "Salvar Alterações"}
        </button>
      </div>
    </form>
  );
}







