"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import Link from "next/link";
import { getBffSession } from "@/lib/bff";
import { getReminderByIdClient } from "@/lib/api/automationApiClient";
import { EditarLembreteClient } from "./EditarLembreteClient";
import { TopBar } from "@/components/TopBar";

export default function EditarLembretePage() {
  const params = useParams<{ id: string }>();
  const reminderId = Array.isArray(params?.id) ? params.id[0] : params?.id;

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [reminder, setReminder] = useState<Awaited<ReturnType<typeof getReminderByIdClient>> | null>(null);

  useEffect(() => {
    let isMounted = true;

    async function load() {
      if (!reminderId) return;

      // Validar se o ID é um GUID válido
      const guidRegex = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
      if (!guidRegex.test(reminderId)) {
        if (!isMounted) return;
        setError("ID do lembrete inválido");
        setLoading(false);
        return;
      }

      try {
        const session = await getBffSession();
        if (!session.authenticated) {
          window.location.href = `/login?returnUrl=${encodeURIComponent(`/automacao/lembretes/${reminderId}/editar`)}`;
          return;
        }

        const r = await getReminderByIdClient(reminderId);
        if (!isMounted) return;
        setReminder(r);
      } catch (e) {
        console.error("Erro ao carregar lembrete:", e);
        if (!isMounted) return;
        const errorMessage = e instanceof Error ? e.message : "Erro ao carregar lembrete";
        // Se for 404, mostrar mensagem mais específica
        if (errorMessage.includes("404") || errorMessage.includes("não encontrado")) {
          setError("Lembrete não encontrado. Ele pode ter sido deletado ou você não tem permissão para visualizá-lo.");
        } else {
          setError(errorMessage);
        }
      } finally {
        if (isMounted) setLoading(false);
      }
    }

    load();
    return () => {
      isMounted = false;
    };
  }, [reminderId]);

  if (!reminderId) return null;

  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-900">
      <TopBar title="Editar Lembrete" showBackButton backHref={`/automacao/lembretes`} />
      <main className="container mx-auto px-4 py-8">
        <div className="max-w-2xl mx-auto">
          <div className="bg-zinc-800 dark:bg-zinc-800 rounded-lg border border-zinc-700 dark:border-zinc-700 shadow-sm p-6">
            {loading ? (
              <p className="text-sm text-zinc-400 dark:text-zinc-400">Carregando...</p>
            ) : error || !reminder ? (
              <div className="rounded-md border border-red-200 dark:border-red-800 bg-red-50 dark:bg-red-900/20 p-4">
                <p className="text-sm text-red-700 dark:text-red-400">{error ?? "Lembrete não encontrado."}</p>
                <Link
                  href={`/automacao/lembretes`}
                  className="mt-3 inline-block text-sm text-red-700 dark:text-red-400 underline"
                >
                  Voltar
                </Link>
              </div>
            ) : (
              <EditarLembreteClient
                reminderId={reminderId}
                initialData={{
                  contactId: reminder.contactId,
                  reason: reminder.reason,
                  suggestedMessage: reminder.suggestedMessage || "",
                  scheduledFor: reminder.scheduledFor,
                  status: reminder.status,
                }}
              />
            )}
          </div>
        </div>
      </main>
    </div>
  );
}





