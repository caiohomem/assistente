"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import Link from "next/link";
import { getBffSession } from "@/lib/bff";
import { getLetterheadByIdClient } from "@/lib/api/automationApiClient";
import { EditarPapelTimbradoClient } from "./EditarPapelTimbradoClient";
import { TopBar } from "@/components/TopBar";

export default function EditarPapelTimbradoPage() {
  const params = useParams<{ id: string }>();
  const letterheadId = params?.id;

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [letterhead, setLetterhead] = useState<Awaited<ReturnType<typeof getLetterheadByIdClient>> | null>(null);

  useEffect(() => {
    let isMounted = true;

    async function load() {
      if (!letterheadId) return;

      try {
        const session = await getBffSession();
        if (!session.authenticated) {
          window.location.href = `/login?returnUrl=${encodeURIComponent(`/automacao/papeis-timbrados/${letterheadId}/editar`)}`;
          return;
        }

        const l = await getLetterheadByIdClient(letterheadId);
        if (!isMounted) return;
        setLetterhead(l);
      } catch (e) {
        console.error("Erro ao carregar papel timbrado:", e);
        if (!isMounted) return;
        setError(e instanceof Error ? e.message : "Erro ao carregar papel timbrado");
      } finally {
        if (isMounted) setLoading(false);
      }
    }

    load();
    return () => {
      isMounted = false;
    };
  }, [letterheadId]);

  if (!letterheadId) return null;

  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-900">
      <TopBar title="Editar Papel Timbrado" showBackButton backHref={`/automacao/papeis-timbrados/${letterheadId}`} />
      <main className="container mx-auto px-4 py-8">
        <div className="max-w-4xl mx-auto">
          <div className="bg-white dark:bg-zinc-800 rounded-lg border border-zinc-200 dark:border-zinc-700 shadow-sm p-6">
            {loading ? (
              <p className="text-sm text-zinc-600 dark:text-zinc-400">Carregando...</p>
            ) : error || !letterhead ? (
              <div className="rounded-md border border-red-200 dark:border-red-800 bg-red-50 dark:bg-red-900/20 p-4">
                <p className="text-sm text-red-700 dark:text-red-400">{error ?? "Papel timbrado não encontrado."}</p>
                <Link
                  href={`/automacao/papeis-timbrados/${letterheadId}`}
                  className="mt-3 inline-block text-sm text-red-700 dark:text-red-400 underline"
                >
                  Voltar
                </Link>
              </div>
            ) : (
              <EditarPapelTimbradoClient
                letterheadId={letterheadId}
                initialData={{
                  name: letterhead.name,
                  designData: letterhead.designData,
                  isActive: letterhead.isActive,
                }}
              />
            )}
          </div>
        </div>
      </main>
    </div>
  );
}





