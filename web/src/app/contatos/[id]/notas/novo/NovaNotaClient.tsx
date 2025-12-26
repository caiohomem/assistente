"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useTranslations } from "next-intl";
import { createTextNoteClient } from "@/lib/api/notesApiClient";
import { CreateTextNoteRequest } from "@/lib/types/note";

interface NovaNotaClientProps {
  contactId: string;
}

export function NovaNotaClient({ contactId }: NovaNotaClientProps) {
  const router = useRouter();
  const t = useTranslations();
  const tNotes = useTranslations("notes");

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [content, setContent] = useState("");
  const [structuredData, setStructuredData] = useState("");
  const [structuredDataError, setStructuredDataError] = useState<string | null>(null);

  const validateJson = (jsonString: string): boolean => {
    if (!jsonString.trim()) return true; // Empty is valid (optional)
    try {
      JSON.parse(jsonString);
      return true;
    } catch {
      return false;
    }
  };

  const handleStructuredDataChange = (value: string) => {
    setStructuredData(value);
    if (value.trim() && !validateJson(value)) {
      setStructuredDataError(tNotes("invalidJson"));
    } else {
      setStructuredDataError(null);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!content.trim()) {
      setError(tNotes("contentRequired"));
      return;
    }

    if (structuredDataError) {
      setError(tNotes("invalidJson"));
      return;
    }

    setLoading(true);
    try {
      const request: CreateTextNoteRequest = {
        text: content.trim(),
        structuredData: structuredData.trim() ? structuredData.trim() : undefined,
      };

      await createTextNoteClient(contactId, request);
      router.push(`/contatos/${contactId}`);
      router.refresh();
    } catch (err: any) {
      setError(err.message || tNotes("errorCreating"));
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

      {/* Content */}
      <div>
        <label className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-2">
          {tNotes("content")} <span className="text-red-500">*</span>
        </label>
        <textarea
          value={content}
          onChange={(e) => setContent(e.target.value)}
          rows={10}
          className="w-full px-3 py-2 border border-zinc-300 dark:border-zinc-600 rounded-md bg-white dark:bg-zinc-700 text-zinc-900 dark:text-zinc-100 focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
          placeholder={tNotes("contentPlaceholder")}
          required
        />
      </div>

      {/* Structured Data */}
      <div>
        <label className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-2">
          {tNotes("structuredData")} <span className="text-zinc-400 text-xs">({t("common.optional")})</span>
        </label>
        <textarea
          value={structuredData}
          onChange={(e) => handleStructuredDataChange(e.target.value)}
          rows={6}
          className={`w-full px-3 py-2 border rounded-md bg-white dark:bg-zinc-700 text-zinc-900 dark:text-zinc-100 focus:ring-2 focus:ring-indigo-500 focus:border-transparent font-mono text-sm ${
            structuredDataError
              ? "border-red-300 dark:border-red-700"
              : "border-zinc-300 dark:border-zinc-600"
          }`}
          placeholder={tNotes("structuredDataPlaceholder")}
        />
        {structuredDataError && (
          <p className="mt-1 text-xs text-red-600 dark:text-red-400">{structuredDataError}</p>
        )}
        <p className="mt-1 text-xs text-zinc-500 dark:text-zinc-400">
          {t("common.optional")} - {tNotes("structuredDataPlaceholder")}
        </p>
      </div>

      {/* Actions */}
      <div className="flex gap-4 pt-4">
        <button
          type="submit"
          disabled={loading || !!structuredDataError}
          className="flex-1 rounded-md bg-zinc-900 dark:bg-zinc-800 px-4 py-2 text-sm font-medium text-white hover:bg-zinc-800 dark:hover:bg-zinc-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
        >
          {loading ? tNotes("creating") : tNotes("create")}
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

