"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { getBffSession } from "@/lib/bff";
import { TopBar } from "@/components/TopBar";
import { NovaNotaAudioClient } from "./NovaNotaAudioClient";

export default function NotasAudioPage() {
  const params = useParams();
  const contactId = params.id as string;
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let isMounted = true;

    async function check() {
      if (!contactId) return;
      try {
        const session = await getBffSession();
        if (!session.authenticated) {
          window.location.href = `/login?returnUrl=${encodeURIComponent(`/contatos/${contactId}/notas-audio`)}`;
          return;
        }
      } finally {
        if (isMounted) setLoading(false);
      }
    }

    check();
    return () => {
      isMounted = false;
    };
  }, [contactId]);

  if (!contactId) return null;

  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-900">
      <TopBar title="Notas de Áudio" showBackButton backHref={`/contatos/${contactId}`} />
      <main className="container mx-auto px-4 py-8">
        <div className="max-w-2xl mx-auto">
          <div className="bg-white dark:bg-zinc-800 rounded-lg border border-zinc-200 dark:border-zinc-700 shadow-sm p-6">
            {loading ? (
              <p className="text-sm text-zinc-600 dark:text-zinc-400">Carregando...</p>
            ) : (
              <NovaNotaAudioClient contactId={contactId} />
            )}
          </div>
        </div>
      </main>
    </div>
  );
}
