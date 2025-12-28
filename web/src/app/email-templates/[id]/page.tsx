"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import Link from "next/link";
import { getBffSession } from "@/lib/bff";
import { getEmailTemplateByIdClient } from "@/lib/api/emailTemplatesApiClient";
import { EmailTemplateDetailsClient } from "./EmailTemplateDetailsClient";
import { TopBar } from "@/components/TopBar";

export default function EmailTemplateDetailsPage() {
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
          window.location.href = `/login?returnUrl=${encodeURIComponent(`/email-templates/${templateId}`)}`;
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
        <TopBar title="Detalhes do Template" showBackButton backHref="/email-templates" />
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
        <TopBar title="Detalhes do Template" showBackButton backHref="/email-templates" />
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
      <TopBar title="Detalhes do Template" showBackButton backHref="/email-templates">
        <Link
          href={`/email-templates/${templateId}/editar`}
          className="inline-flex items-center justify-center rounded-md bg-indigo-600 dark:bg-indigo-500 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700 dark:hover:bg-indigo-600 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2"
        >
          Editar
        </Link>
      </TopBar>
      <main className="container mx-auto px-4 py-8">
        <div className="max-w-4xl mx-auto">
          <EmailTemplateDetailsClient template={template} />
        </div>
      </main>
    </div>
  );
}

