"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import Link from "next/link";
import { getBffSession } from "@/lib/bff";
import { getEmailTemplateByIdClient } from "@/lib/api/emailTemplatesApiClient";
import { EditarEmailTemplateClient } from "./EditarEmailTemplateClient";
import { TopBar } from "@/components/TopBar";

export default function EditarEmailTemplatePage() {
  const params = useParams<{ id: string }>();
  const templateId = params?.id;

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [template, setTemplate] = useState<Awaited<ReturnType<typeof getEmailTemplateByIdClient>> | null>(null);

  useEffect(() => {
    let isMounted = true;

    async function load() {
      if (!templateId) return;

      try {
        const session = await getBffSession();
        if (!session.authenticated) {
          window.location.href = `/login?returnUrl=${encodeURIComponent(`/email-templates/${templateId}/editar`)}`;
          return;
        }

        const t = await getEmailTemplateByIdClient(templateId);
        if (!isMounted) return;
        setTemplate(t);
      } catch (e) {
        console.error("Erro ao carregar template de email:", e);
        if (!isMounted) return;
        setError(e instanceof Error ? e.message : "Erro ao carregar template de email");
      } finally {
        if (isMounted) setLoading(false);
      }
    }

    load();
    return () => {
      isMounted = false;
    };
  }, [templateId]);

  if (!templateId) return null;

  if (loading) {
    return (
      <div className="min-h-screen bg-zinc-50 dark:bg-zinc-900">
        <TopBar title="Editar Template de Email" showBackButton backHref={`/email-templates/${templateId}`} />
        <div className="mx-auto max-w-4xl px-4 py-8 sm:px-6 lg:px-8">
          <div className="text-center">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600 dark:border-indigo-400 mx-auto mb-4"></div>
            <p className="text-gray-600 dark:text-gray-300">Carregando template...</p>
          </div>
        </div>
      </div>
    );
  }

  if (error || !template) {
    return (
      <div className="min-h-screen bg-zinc-50 dark:bg-zinc-900">
        <TopBar title="Editar Template de Email" showBackButton backHref="/email-templates" />
        <div className="mx-auto max-w-4xl px-4 py-8 sm:px-6 lg:px-8">
          <div className="rounded-md bg-red-50 dark:bg-red-900/20 p-4">
            <p className="text-sm text-red-800 dark:text-red-200">
              {error || "Template não encontrado"}
            </p>
            <Link
              href="/email-templates"
              className="mt-2 inline-block text-sm text-red-600 dark:text-red-400 hover:text-red-700 dark:hover:text-red-300"
            >
              Voltar para lista
            </Link>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-900">
      <TopBar title="Editar Template de Email" showBackButton backHref={`/email-templates/${templateId}`} />
      <main className="container mx-auto px-4 py-8">
        <div className="max-w-4xl mx-auto">
          <div className="bg-zinc-800 dark:bg-zinc-800 rounded-lg border border-zinc-700 dark:border-zinc-700 shadow-sm p-6">
            <EditarEmailTemplateClient templateId={templateId} initialData={template} />
          </div>
        </div>
      </main>
    </div>
  );
}

