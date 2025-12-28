"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import Link from "next/link";
import { getBffSession } from "@/lib/bff";
import { getDraftByIdClient } from "@/lib/api/automationApiClient";
import { EditarRascunhoClient } from "./EditarRascunhoClient";
import { TopBar } from "@/components/TopBar";

export default function EditarRascunhoPage() {
  const params = useParams<{ id: string }>();
  const draftId = params?.id;

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [draft, setDraft] = useState<Awaited<ReturnType<typeof getDraftByIdClient>> | null>(null);

  useEffect(() => {
    let isMounted = true;

    async function load() {
      if (!draftId) return;

      try {
        const session = await getBffSession();
        if (!session.authenticated) {
          window.location.href = `/login?returnUrl=${encodeURIComponent(`/automacao/rascunhos/${draftId}/editar`)}`;
          return;
        }

        const d = await getDraftByIdClient(draftId);
        if (!isMounted) return;
        setDraft(d);
      } catch (e) {
        console.error("Erro ao carregar rascunho:", e);
        if (!isMounted) return;
        setError(e instanceof Error ? e.message : "Erro ao carregar rascunho");
      } finally {
        if (isMounted) setLoading(false);
      }
    }

    load();
    return () => {
      isMounted = false;
    };
  }, [draftId]);

  if (!draftId) return null;

  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-900">
      <TopBar title="Editar Rascunho" showBackButton backHref={`/automacao/rascunhos/${draftId}`} />
      <main className="container mx-auto px-4 py-8">
        <div className="max-w-4xl mx-auto">
          <div className="bg-zinc-800 dark:bg-zinc-800 rounded-lg border border-zinc-700 dark:border-zinc-700 shadow-sm p-6">
            {loading ? (
              <p className="text-sm text-zinc-400 dark:text-zinc-400">Carregando...</p>
            ) : error || !draft ? (
              <div className="rounded-md border border-red-200 dark:border-red-800 bg-red-50 dark:bg-red-900/20 p-4">
                <p className="text-sm text-red-700 dark:text-red-400">{error ?? "Rascunho não encontrado."}</p>
                <Link
                  href={`/automacao/rascunhos/${draftId}`}
                  className="mt-3 inline-block text-sm text-red-700 dark:text-red-400 underline"
                >
                  Voltar
                </Link>
              </div>
            ) : (
              <EditarRascunhoClient
                draftId={draftId}
                initialData={{
                  documentType: draft.documentType,
                  content: draft.content,
                  contactId: draft.contactId || "",
                  templateId: draft.templateId || "",
                  letterheadId: draft.letterheadId || "",
                  status: draft.status,
                }}
              />
            )}
          </div>
        </div>
      </main>
    </div>
  );
}





