"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import Link from "next/link";
import { getBffSession } from "@/lib/bff";
import { getTemplateByIdClient } from "@/lib/api/automationApiClient";
import { EditarTemplateClient } from "./EditarTemplateClient";
import { TopBar } from "@/components/TopBar";

export default function EditarTemplatePage() {
  const params = useParams<{ id: string }>();
  const templateId = params?.id;

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [template, setTemplate] = useState<Awaited<ReturnType<typeof getTemplateByIdClient>> | null>(null);

  useEffect(() => {
    let isMounted = true;

    async function load() {
      if (!templateId) return;

      try {
        const session = await getBffSession();
        if (!session.authenticated) {
          window.location.href = `/login?returnUrl=${encodeURIComponent(`/automacao/templates/${templateId}/editar`)}`;
          return;
        }

        const t = await getTemplateByIdClient(templateId);
        if (!isMounted) return;
        setTemplate(t);
      } catch (e) {
        console.error("Erro ao carregar template:", e);
        if (!isMounted) return;
        setError(e instanceof Error ? e.message : "Erro ao carregar template");
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

  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-900">
      <TopBar title="Editar Template" showBackButton backHref={`/automacao/templates/${templateId}`} />
      <main className="container mx-auto px-4 py-8">
        <div className="max-w-4xl mx-auto">
          <div className="bg-white dark:bg-zinc-800 rounded-lg border border-zinc-200 dark:border-zinc-700 shadow-sm p-6">
            {loading ? (
              <p className="text-sm text-zinc-600 dark:text-zinc-400">Carregando...</p>
            ) : error || !template ? (
              <div className="rounded-md border border-red-200 dark:border-red-800 bg-red-50 dark:bg-red-900/20 p-4">
                <p className="text-sm text-red-700 dark:text-red-400">{error ?? "Template não encontrado."}</p>
                <Link
                  href={`/automacao/templates/${templateId}`}
                  className="mt-3 inline-block text-sm text-red-700 dark:text-red-400 underline"
                >
                  Voltar
                </Link>
              </div>
            ) : (
              <EditarTemplateClient
                templateId={templateId}
                initialData={{
                  name: template.name,
                  type: template.type,
                  body: template.body,
                  placeholdersSchema: template.placeholdersSchema || "",
                  active: template.active,
                }}
              />
            )}
          </div>
        </div>
      </main>
    </div>
  );
}





